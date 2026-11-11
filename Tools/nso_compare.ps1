# Compares same-named PNGs in two folders. A pixel "differs" when any channel is off by more than $Tolerance.
# Exit code 1 if any pair differs on more than $MaxPercent of its pixels.
param([string]$A, [string]$B, [int]$Tolerance = 24, [double]$MaxPercent = 2.0)
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @"
using System; using System.Drawing; using System.Drawing.Imaging;
public static class PngDiff {
  public static double Percent(string pa, string pb, int tol) {
    using (var a = new Bitmap(pa)) using (var b = new Bitmap(pb)) {
      if (a.Width != b.Width || a.Height != b.Height) return 100.0;
      var r = new Rectangle(0, 0, a.Width, a.Height);
      var da = a.LockBits(r, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
      var db = b.LockBits(r, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
      int n = a.Width * a.Height * 4; var ba = new byte[n]; var bb = new byte[n];
      System.Runtime.InteropServices.Marshal.Copy(da.Scan0, ba, 0, n);
      System.Runtime.InteropServices.Marshal.Copy(db.Scan0, bb, 0, n);
      a.UnlockBits(da); b.UnlockBits(db);
      long bad = 0;
      for (int i = 0; i < n; i += 4)
        if (Math.Abs(ba[i] - bb[i]) > tol || Math.Abs(ba[i+1] - bb[i+1]) > tol || Math.Abs(ba[i+2] - bb[i+2]) > tol) bad++;
      return 100.0 * bad / (a.Width * a.Height);
    }
  }
}
"@ -ReferencedAssemblies System.Drawing
$failed = $false
foreach ($f in Get-ChildItem $A -Filter *.png) {
  $other = Join-Path $B $f.Name
  if (-not (Test-Path $other)) { "{0,-24} MISSING in B" -f $f.Name; $failed = $true; continue }
  $p = [PngDiff]::Percent($f.FullName, $other, $Tolerance)
  $status = if ($p -gt $MaxPercent) { "DIFF"; $failed = $true } else { "ok" }
  "{0,-24} {1,6:N2}% pixels differ  {2}" -f $f.Name, $p, $status
}
if ($failed) { exit 1 }
