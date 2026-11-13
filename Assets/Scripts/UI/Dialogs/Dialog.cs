public abstract class Dialog
{
    public Command left;
    public Command center;
    public Command right;
    private int lenCaption;
    // Offline addition (user request): with two or more bottom buttons (yes/no and similar),
    // Left/Right arrows (keyboard, gamepad) choose a button and Enter presses it.
    // Null = nothing chosen yet: Enter keeps the original behaviour (centre button).
    private Command keySel;
    /// <summary>Bottom buttons in screen order (left, centre, right), skipping missing ones.</summary>
    private int ButtonsInOrder(Command[] buf)
    {
        int n = 0;
        if (left != null)
            buf[n++] = left;
        if (center != null)
            buf[n++] = center;
        if (right != null)
            buf[n++] = right;
        return n;
    }

    private static readonly Command[] orderBuf = new Command[3];
    // ---- Shared input handling (offline addition, user request: one base class instead of per-dialog fixes) ----
    // Every dialog gets, from this base class:
    //   * Left/Right arrows (keyboard, gamepad) choose a bottom button, Enter presses it;
    //   * typed keys go to the dialog's focused text field (FocusedField), and that field stays focused
    //     (a stray mouse release no longer unfocuses it);
    //   * Up/Down step a numeric field by NumberStep (gamepad has no digit keys), unless the dialog
    //     uses Up/Down itself (UpDownStepsNumber = false, e.g. Input2Dlg switches fields with them).
    // A dialog with text fields only overrides FocusedField (and UpDownStepsNumber if needed).
    /// <summary>Amount Up/Down add to or remove from a numeric field. Tunable.</summary>
    public static int NumberStep = 1;
    /// <summary>Smallest value Down can reach in a numeric field. Tunable.</summary>
    public static int NumberMin = 1;
    /// <summary>The text field that receives typed keys; null = the dialog has none.</summary>
    protected virtual TField FocusedField
    {
        get
        {
            return null;
        }
    }

    /// <summary>False when the dialog uses Up/Down for something else.</summary>
    protected virtual bool UpDownStepsNumber
    {
        get
        {
            return true;
        }
    }

    /// <summary>Adds dir * NumberStep to a numeric field (empty counts as 0), never below NumberMin or past its max length.</summary>
    private void StepNumber(int dir)
    {
        TField tf = FocusedField;
        if (tf == null || !UpDownStepsNumber || tf.getIputType() != TField.INPUT_TYPE_NUMERIC)
            return;
        long value;
        if (!long.TryParse(tf.getText(), out value))
            value = 0;
        value = System.Math.Max(NumberMin, value + dir * NumberStep);
        string s = value.ToString();
        if (s.Length <= tf.getMaxTextLenght())
            tf.setText(s);
    }

    /// <summary>Moves the arrow-key selection one button left (-1) or right (+1), clamped at the ends.</summary>
    private void MoveSelection(int dir)
    {
        int n = ButtonsInOrder(orderBuf);
        if (n < 2)
            return;
        int i = System.Array.IndexOf(orderBuf, keySel, 0, n);
        if (i < 0)
            i = dir > 0 ? 0 : n - 1; // first press selects the end button on that side
        else
            i = System.Math.Max(0, System.Math.Min(n - 1, i + dir));
        keySel = orderBuf[i];
    }

    public virtual void paint(mGraphics g)
    {
        g.translate(-g.getTranslateX(), -g.getTranslateY());
        g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
        if (keySel != null && keySel != left && keySel != center && keySel != right)
            keySel = null; // buttons changed
        Command.keySelected = keySel;
        GameCanvas.paintz.paintCmdBar(g, left, center, right);
        Command.keySelected = null;
    }

    public virtual void keyPress(int keyCode)
    {
        TField field = FocusedField;
        if (field != null)
            field.keyPressed(keyCode); // typed text (was done by each input dialog)
        switch (keyCode)
        {
            case -38:
            case -1:
                GameCanvas.keyHold[2] = true;
                GameCanvas.keyPressedz[2] = true;
                StepNumber(1);
                break;
            case -39:
            case -2:
                GameCanvas.keyHold[8] = true;
                GameCanvas.keyPressedz[8] = true;
                StepNumber(-1);
                break;
            case -21:
            case -6:
                GameCanvas.keyHold[12] = true;
                GameCanvas.keyPressedz[12] = true;
                break;
            case -22:
            case -7:
                GameCanvas.keyHold[13] = true;
                GameCanvas.keyPressedz[13] = true;
                break;
            case -3: // Left arrow: offline addition, choose the bottom button on the left
                MoveSelection(-1);
                break;
            case -4: // Right arrow
                MoveSelection(1);
                break;
            case -5:
            case 10:
                GameCanvas.keyHold[5] = true;
                GameCanvas.keyPressedz[5] = true;
                break;
            case 119:
                GameCanvas.keyHold[16] = true;
                GameCanvas.keyPressedz[16] = true;
                break;
            case 115:
                GameCanvas.keyHold[17] = true;
                GameCanvas.keyPressedz[17] = true;
                break;
            case 97:
                GameCanvas.keyHold[18] = true;
                GameCanvas.keyPressedz[18] = true;
                break;
            case 100:
                GameCanvas.keyHold[19] = true;
                GameCanvas.keyPressedz[19] = true;
                break;
        }
    }

    public virtual void update()
    {
        TField field = FocusedField;
        if (field != null)
        {
            field.update();
            field.isFocus = true; // keep typing enabled even after a click outside the field
        }

        if (keySel != null && keySel != left && keySel != center && keySel != right)
            keySel = null; // buttons changed
        if (keySel != null && GameCanvas.keyPressedz[5])
        {
            // Enter presses the button chosen with the arrows.
            Command chosen = keySel;
            keySel = null;
            GameCanvas.keyPressedz[5] = false;
            mScreen.keyTouch = -1;
            chosen.performAction();
            Sound.play(Sound.MBClick, 0.5f);
            GameCanvas.clearKeyPressed();
            GameCanvas.clearKeyHold();
            return;
        }

        if (center != null && (GameCanvas.keyPressedz[5] || mScreen.getCmdPointerLast(center)))
        {
            GameCanvas.keyPressedz[5] = false;
            GameCanvas.isPointerClick = false;
            mScreen.keyTouch = -1;
            GameCanvas.isPointerJustRelease = false;
            if (center != null)
            {
                center.performAction();
            }

            mScreen.keyTouch = -1;
            Sound.play(Sound.MBClick, 0.5f);
        }

        if (left != null && (GameCanvas.keyPressedz[12] || mScreen.getCmdPointerLast(left)))
        {
            GameCanvas.keyPressedz[12] = false;
            GameCanvas.isPointerClick = false;
            mScreen.keyTouch = -1;
            GameCanvas.isPointerJustRelease = false;
            if (left != null)
            {
                left.performAction();
            }

            mScreen.keyTouch = -1;
            Sound.play(Sound.MBClick, 0.5f);
        }

        if (right != null && (GameCanvas.keyPressedz[13] || mScreen.getCmdPointerLast(right)))
        {
            GameCanvas.keyPressedz[13] = false;
            GameCanvas.isPointerClick = false;
            GameCanvas.isPointerJustRelease = false;
            mScreen.keyTouch = -1;
            if (right != null)
            {
                right.performAction();
            }

            mScreen.keyTouch = -1;
            Sound.play(Sound.MBClick, 0.5f);
        }

        GameCanvas.clearKeyPressed();
        GameCanvas.clearKeyHold();
    }

    public virtual void show()
    {
    }
}
