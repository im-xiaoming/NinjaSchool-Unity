using System;
using System.Collections.Generic;
using System.IO;
using NsoServer.Data;

namespace NsoServer.Ai.Rl
{
    using Math = System.Math;

    /// <summary>Small CPU MLP exported from sb3-contrib MaskablePPO. No Python/Unity calls in inference.</summary>
    public sealed class RlPolicy
    {
        public string Version, Domain;
        public int Stage;
        private float[][] actor1, actor2, actorOut, critic1, critic2, criticOut;
        private float[] ab1, ab2, abOut, cb1, cb2, cbOut;

        public static RlPolicy Load(string path, string domain)
        {
            if (new FileInfo(path).Length > MaxFileBytes) throw new FormatException("Policy exceeds " + MaxFileBytes / 1000000 + " MB");
            var d = Json.Parse(File.ReadAllText(path)) as Dictionary<string, object>;
            if (d == null || Convert.ToInt32(d["schema"]) != Observation.Schema ||
                Convert.ToInt32(d["inputs"]) != Observation.Size || Convert.ToInt32(d["actions"]) != Observation.ActionCount ||
                Convert.ToString(d["domain"]) != domain) throw new FormatException("Incompatible policy schema");
            var p = new RlPolicy { Version = Convert.ToString(d["version"]), Domain = domain, Stage = Convert.ToInt32(d["stage"]) };
            // Hidden widths come from the file (older models: 64; trainer.py HIDDEN for new ones), every shape is checked.
            int h1 = Width(d, "ab1"), h2 = Width(d, "ab2"), v1 = Width(d, "cb1"), v2 = Width(d, "cb2");
            p.actor1 = Matrix(d, "a1", h1, Observation.Size); p.ab1 = Vector(d, "ab1", h1);
            p.actor2 = Matrix(d, "a2", h2, h1); p.ab2 = Vector(d, "ab2", h2);
            p.actorOut = Matrix(d, "ao", Observation.ActionCount, h2); p.abOut = Vector(d, "abo", Observation.ActionCount);
            p.critic1 = Matrix(d, "c1", v1, Observation.Size); p.cb1 = Vector(d, "cb1", v1);
            p.critic2 = Matrix(d, "c2", v2, v1); p.cb2 = Vector(d, "cb2", v2);
            p.criticOut = Matrix(d, "co", 1, v2); p.cbOut = Vector(d, "cbo", 1);
            if (p.Version.Length == 0 || p.Stage < 1 || p.Stage > 3) throw new FormatException("Invalid policy identity");
            return p;
        }

        /// <summary>Largest accepted policy file (a 256x256 actor and critic is about 8 MB of JSON).</summary>
        public const long MaxFileBytes = 64000000;
        private const int MaxHidden = 1024;

        private static int Width(Dictionary<string, object> d, string key)
        {
            var list = d.ContainsKey(key) ? d[key] as List<object> : null;
            if (list == null || list.Count < 1 || list.Count > MaxHidden) throw new FormatException("Bad tensor " + key);
            return list.Count;
        }

        private static float[] Vector(Dictionary<string, object> d, string key, int n)
        {
            var list = d[key] as List<object>;
            if (list == null || list.Count != n) throw new FormatException("Bad tensor " + key);
            var v = new float[n];
            for (int i = 0; i < n; i++) { v[i] = Convert.ToSingle(list[i]); if (float.IsNaN(v[i]) || float.IsInfinity(v[i])) throw new FormatException("Nonfinite tensor"); }
            return v;
        }
        private static float[][] Matrix(Dictionary<string, object> d, string key, int rows, int cols)
        {
            var list = d[key] as List<object>;
            if (list == null || list.Count != rows) throw new FormatException("Bad tensor " + key);
            var m = new float[rows][];
            for (int i = 0; i < rows; i++) m[i] = Vector(new Dictionary<string, object> { { "row", list[i] } }, "row", cols);
            return m;
        }
        private static float[] Layer(float[] x, float[][] w, float[] b, bool tanh)
        {
            var y = new float[b.Length];
            for (int i = 0; i < y.Length; i++)
            {
                float sum = b[i];
                for (int j = 0; j < x.Length; j++) sum += w[i][j] * x[j];
                y[i] = tanh ? (float)Math.Tanh(sum) : sum;
            }
            return y;
        }
        public double Value(float[] x) { return Layer(Layer(Layer(x, critic1, cb1, true), critic2, cb2, true), criticOut, cbOut, false)[0]; }
        public double[] Probabilities(Observation o)
        {
            var logits = Layer(Layer(Layer(o.Values, actor1, ab1, true), actor2, ab2, true), actorOut, abOut, false);
            double max = double.NegativeInfinity, total = 0;
            for (int i = 0; i < logits.Length; i++) if (o.ActionMask[i]) max = Math.Max(max, logits[i]);
            var probs = new double[logits.Length];
            for (int i = 0; i < probs.Length; i++) if (o.ActionMask[i]) { probs[i] = Math.Exp(logits[i] - max); total += probs[i]; }
            if (total <= 0 || double.IsNaN(total)) throw new InvalidOperationException("No finite valid policy action");
            for (int i = 0; i < probs.Length; i++) probs[i] /= total;
            return probs;
        }
        public int Sample(Observation o, Random rng, out double logp, out double value)
        {
            var p = Probabilities(o); double r = rng.NextDouble(); int choice = 0;
            for (int i = 0; i < p.Length; i++) if (o.ActionMask[i]) { choice = i; r -= p[i]; if (r <= 0) break; }
            logp = Math.Log(Math.Max(1e-30, p[choice])); value = Value(o.Values); return choice;
        }
    }
}
