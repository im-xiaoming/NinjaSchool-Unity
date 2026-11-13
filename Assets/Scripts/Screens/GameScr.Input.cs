using Functions;
using Functions.Auto;
using System;
using System.Threading;
using UnityEngine;

public partial class GameScr
{
    private void loadDefaultKeySkill()
    {
        Out.println("LOAD DEFAULT KEY SKILL");
        for (int i = 0; i < keySkill.Length && i < Char.getMyChar().vSkillFight.size(); i++)
        {
            Skill skill = (Skill)Char.getMyChar().vSkillFight.elementAt(i);
            keySkill[i] = skill;
        }

        saveKeySkillToRMS();
    }

    protected void doSetKeySkill(SkillTemplate skillTemplate)
    {
        Skill skill = Char.getMyChar().getSkill(skillTemplate);
        string[] array = ((!TField.isQwerty) ? mResources.KEY_T9 : mResources.KEY_QWERTY);
        MyVector myVector = new MyVector();
        for (int i = 0; i < 3; i++)
        {
            myVector.addElement(new Command(p: new object[2] { skill, i + string.Empty }, caption: array[i], action: 11121));
        }

        GameCanvas.menu.startAt(myVector, 0);
    }

    protected void saveKeySkillToRMS()
    {
        sbyte[] array = new sbyte[keySkill.Length];
        for (int i = 0; i < keySkill.Length; i++)
        {
            if (keySkill[i] == null)
            {
                array[i] = -1;
            }
            else
            {
                array[i] = keySkill[i].template.id;
            }
        }

        if (Char.getMyChar().isHumanz())
        {
            Service.gI().saveRms("KSkill", array, 0);
        }
        else
        {
            Service.gI().saveRms("KSkill", array, 1);
        }
    }

    public override void keyPress(int keyCode)
    {
        if (tfText != null && tfText.isFocus)
        {
            tfText.keyPressed(keyCode);
        }

        base.keyPress(keyCode);
    }

    public override void updateKey()
    {
        if (InfoDlg.isLock)
        {
            return;
        }

        if (GameCanvas.isTouch && !ChatTextField.gI().isShow && !GameCanvas.menu.showMenu)
        {
            updateKeyTouchControl();
        }

        checkAuto();
        showCharList();
        GameCanvas.debug("F2", 0);
        if (ChatPopup.currentMultilineChatPopup != null)
        {
            Command cmdNextLine = ChatPopup.currentMultilineChatPopup.cmdNextLine;
            if ((GameCanvas.keyPressedz[5] || mScreen.getCmdPointerLast(cmdNextLine)) && cmdNextLine != null)
            {
                GameCanvas.isPointerJustRelease = false;
                GameCanvas.keyPressedz[5] = false;
                mScreen.keyTouch = -1;
                cmdNextLine?.performAction();
            }
        }
        else if (!ChatTextField.gI().isShow)
        {
            if ((GameCanvas.keyPressedz[12] || mScreen.getCmdPointerLast(GameCanvas.currentScreen.left)) && left != null)
            {
                GameCanvas.isPointerJustRelease = false;
                GameCanvas.isPointerClick = false;
                GameCanvas.keyPressedz[12] = false;
                mScreen.keyTouch = -1;
                if (left != null)
                {
                    left.performAction();
                }

                Sound.play(Sound.MBClick, 0.5f);
            }

            if ((GameCanvas.keyPressedz[13] || mScreen.getCmdPointerLast(GameCanvas.currentScreen.right)) && right != null)
            {
                GameCanvas.isPointerJustRelease = false;
                GameCanvas.isPointerClick = false;
                GameCanvas.keyPressedz[13] = false;
                mScreen.keyTouch = -1;
                if (right != null)
                {
                    right.performAction();
                }

                Sound.play(Sound.MBClick, 0.5f);
            }

            // Offline controller fix to GameScr.updateKey: upgrade selection uses the existing
            // left action (select/material/menu/confirm), rather than the optional info-view command.
            if (GameCanvas.keyPressedz[5] && isPaintUpGrade && left != null)
            {
                GameCanvas.keyPressedz[5] = false;
                mScreen.keyTouch = -1;
                left.performAction();
                Sound.play(Sound.MBClick, 0.5f);
            }
            else if ((GameCanvas.keyPressedz[5] || mScreen.getCmdPointerLast(GameCanvas.currentScreen.center)) && center != null)
            {
                GameCanvas.isPointerJustRelease = false;
                GameCanvas.keyPressedz[5] = false;
                mScreen.keyTouch = -1;
                if (center != null)
                {
                    center.performAction();
                }
            }
            // Offline addition (user request): on large screens grid shops have no center command
            // (item info is shown automatically), so Enter / gamepad Circle did nothing. Enter now
            // runs the left "Buy" command, which opens the same buy popup as clicking it.
            // Same for the auto panel (Tính năng > Tự động): Enter / Circle toggles the selected row
            // (doAutoSelect, the action of its "Chọn" command 1510).
            else if (GameCanvas.keyPressedz[5] && center == null && isPaintAuto && indexTitle == 1 && indexRow >= 0)
            {
                GameCanvas.keyPressedz[5] = false;
                mScreen.keyTouch = -1;
                doAutoSelect();
                Sound.play(Sound.MBClick, 0.5f);
            }
            else if (GameCanvas.keyPressedz[5] && center == null && left != null && isPaintUI() && indexTitle >= 1 && IsGridShop)
            {
                GameCanvas.keyPressedz[5] = false;
                mScreen.keyTouch = -1;
                left.performAction();
                Sound.play(Sound.MBClick, 0.5f);
            }
            // Offline addition (user request): in the bag (Thông tin > Hành trang) on large screens the
            // focused item's info panel only has the left "Sử dụng" command (11040 -> actleftItemInfo),
            // so Enter / gamepad Circle did nothing. When moving with the arrows / D-pad, setLCR leaves
            // the left command as "Chọn" (11022) instead, so both cases are covered: Enter opens the
            // original "Chọn" option menu (actBagSelectItem: Sử dụng, Vứt, Tự dùng...). On PC Menu.startAt
            // pre-selects the first entry, "Sử dụng", so a second Enter uses the item (user request).
            else if (GameCanvas.keyPressedz[5] && center == null && left != null && (left.idAction == 11040 || left.idAction == 11022) && isPaintInfoMe && indexMenu == 0 && indexTitle >= 1 && getItemFocus(3) != null)
            {
                GameCanvas.keyPressedz[5] = false;
                mScreen.keyTouch = -1;
                actBagSelectItem();
                Sound.play(Sound.MBClick, 0.5f);
            }
            // Offline addition (user request): in Trang bị (indexMenu 4) on large screens the item info is
            // shown automatically and there is no center command, so Enter / gamepad Circle did nothing.
            // Enter now runs the left "Chọn" command (11082 -> actTrangBiSelect: "Cất vào hành trang"...).
            else if (GameCanvas.keyPressedz[5] && center == null && left != null && left.idAction == 11082 && isPaintInfoMe && indexMenu == 4 && indexTitle >= 1)
            {
                GameCanvas.keyPressedz[5] = false;
                mScreen.keyTouch = -1;
                actTrangBiSelect();
                Sound.play(Sound.MBClick, 0.5f);
            }
            // Thú cưỡi (indexMenu 5), same idea: Enter opens a menu with the left "Tháo ra" command (1516).
            else if (GameCanvas.keyPressedz[5] && center == null && left != null && left.idAction == 1516 && isPaintInfoMe && indexMenu == 5 && indexTitle >= 1)
            {
                GameCanvas.keyPressedz[5] = false;
                mScreen.keyTouch = -1;
                MyVector myVector = new MyVector();
                myVector.addElement(new Command(mResources.MOVEOUT, 1516));
                GameCanvas.menu.startAt(myVector, 3);
                Sound.play(Sound.MBClick, 0.5f);
            }
            // Vĩ thú list (indexMenu 6, indexBijuuMenu 0): Enter opens a menu with "Tháo ra" (508).
            else if (GameCanvas.keyPressedz[5] && center == null && left != null && left.idAction == 508 && isPaintInfoMe && indexMenu == 6 && indexBijuuMenu == 0 && indexTitle >= 1)
            {
                GameCanvas.keyPressedz[5] = false;
                mScreen.keyTouch = -1;
                MyVector myVector = new MyVector();
                myVector.addElement(new Command(mResources.MOVEOUT, 508));
                GameCanvas.menu.startAt(myVector, 3);
                Sound.play(Sound.MBClick, 0.5f);
            }
            // Empty bag cell: the only command is the left "Sắp xếp" (110221); Enter / Circle runs it.
            else if (GameCanvas.keyPressedz[5] && center == null && left != null && left.idAction == 110221 && isPaintInfoMe && indexMenu == 0 && indexTitle >= 1)
            {
                GameCanvas.keyPressedz[5] = false;
                mScreen.keyTouch = -1;
                left.performAction();
                Sound.play(Sound.MBClick, 0.5f);
            }
        }
        else if (!Main.isPC)
        {
            if (ChatTextField.gI().left != null && (GameCanvas.keyPressedz[12] || mScreen.getCmdPointerLast(ChatTextField.gI().left)) && ChatTextField.gI().left != null)
            {
                ChatTextField.gI().left.performAction();
            }

            if (ChatTextField.gI().right != null && (GameCanvas.keyPressedz[13] || mScreen.getCmdPointerLast(ChatTextField.gI().right)) && ChatTextField.gI().right != null)
            {
                ChatTextField.gI().right.performAction();
            }

            if (ChatTextField.gI().center != null && (GameCanvas.keyPressedz[5] || mScreen.getCmdPointerLast(ChatTextField.gI().center)) && ChatTextField.gI().center != null)
            {
                ChatTextField.gI().center.performAction();
            }
        }
        else
        {
            if (ChatTextField.gI().right != null && (GameCanvas.keyPressedz[13] || mScreen.getCmdPointerLast(ChatTextField.gI().right)))
            {
                if (ChatTextField.gI().right != null)
                {
                    ChatTextField.gI().right.performAction();
                }

                Sound.play(Sound.MBClick, 0.5f);
                GameCanvas.clearKeyHold();
                GameCanvas.clearKeyPressed();
            }

            if (ChatTextField.gI().right != null && GameCanvas.keyPressedz[5])
            {
                Out.println("chat>>>>>>>>>>>> " + auto);
                ChatTextField.gI().cmdFireChat.performAction();
                GameCanvas.keyPressedz[5] = false;
                Sound.play(Sound.MBClick, 0.5f);
                GameCanvas.clearKeyHold();
                GameCanvas.clearKeyPressed();
                auto = saveAuto;
                return;
            }
        }

        updateKeySelectZone();
        GameCanvas.debug("F4", 0);
        updateSelectList();
        updateKeyBuyItemUI();
        GameCanvas.debug("F5", 0);
        updateKeyUIInforMe();
        updateKeyClanInfo();
        updateKeyTask();
        GameCanvas.debug("F6", 0);
        updateKeyAlert();
        updateSelectList();
        GameCanvas.debug("F7", 0);
        if (Char.getMyChar().currentMovePoint != null)
        {
            for (int i = 0; i < GameCanvas.keyPressedz.Length; i++)
            {
                if (GameCanvas.keyPressedz[i])
                {
                    Char.getMyChar().currentMovePoint = null;
                    break;
                }
            }
        }

        GameCanvas.debug("F8", 0);
        if (ChatTextField.gI().isShow && GameCanvas.keyAsciiPress != 0)
        {
            ChatTextField.gI().keyPressed(GameCanvas.keyAsciiPress);
            GameCanvas.keyAsciiPress = 0;
        }

        if (isLockKey)
        {
            GameCanvas.clearKeyHold();
            GameCanvas.clearKeyPressed();
            return;
        }

        GameCanvas.debug("F9", 0);
        if (GameCanvas.menu.showMenu || isOpenUI() || Char.isLockKey)
        {
            return;
        }

        if (GameCanvas.keyPressedz[(!Main.isPC) ? 10 : ((!GameCanvas.isChangMapKey) ? 6 : 30)])
        {
            GameCanvas.keyPressedz[(!Main.isPC) ? 10 : ((!GameCanvas.isChangMapKey) ? 6 : 30)] = false;
            Sound.play(Sound.MBClick, 0.5f);
            doUseHP();
        }

        if (GameCanvas.keyPressedz[(!Main.isPC) ? 11 : ((!GameCanvas.isChangMapKey) ? 7 : 31)])
        {
            GameCanvas.keyPressedz[(!Main.isPC) ? 11 : ((!GameCanvas.isChangMapKey) ? 7 : 31)] = false;
            Sound.play(Sound.MBClick, 0.5f);
            doUseMP();
        }

        if (Char.getMyChar().skillPaint != null)
        {
            return;
        }

        if (Main.isPC && ChatTextField.gI().isShow)
        {
            GameCanvas.clearKeyPressed();
            return;
        }

        if (!Char.getMyChar().isCaptcha)
        {
            if (Char.getMyChar().statusMe == 1)
            {
                GameCanvas.debug("F10", 0);
                if (GameCanvas.keyPressedz[5] || GameCanvas.keyPressedz[20])
                {
                    GameCanvas.keyPressedz[5] = false;
                    GameCanvas.keyPressedz[20] = false;
                    doFire(false);
                }
                else if (GameCanvas.keyHold[Key.NUM2] || GameCanvas.keyHold[24])
                {
                    if (!Char.getMyChar().isLockMove && !Char.getMyChar().isBlinking)
                    {
                        setCharJump(0);
                    }
                }
                else if (GameCanvas.keyHold[Key.NUM1])
                {
                    Char.getMyChar().cdir = -1;
                    if (!Char.getMyChar().isLockMove && !Char.getMyChar().isBlinking)
                    {
                        setCharJump(-4);
                    }
                }
                else if (GameCanvas.keyHold[Key.NUM3])
                {
                    Char.getMyChar().cdir = 1;
                    if (!Char.getMyChar().isLockMove && !Char.getMyChar().isBlinking)
                    {
                        setCharJump(4);
                    }
                }
                else if (GameCanvas.keyHold[Key.NUM4] || GameCanvas.keyHold[23])
                {
                    Char.getMyChar().isAttack = false;
                    if (Char.getMyChar().cdir == 1)
                    {
                        Char.getMyChar().cdir = -1;
                    }
                    else if (!Char.getMyChar().isLockMove && !Char.getMyChar().isBlinking)
                    {
                        Char.getMyChar().statusMe = 2;
                        Char.getMyChar().cvx = -Char.getMyChar().getSpeed();
                    }
                }
                else if (GameCanvas.keyHold[Key.NUM6] || GameCanvas.keyHold[22])
                {
                    Char.getMyChar().isAttack = false;
                    if (Char.getMyChar().cdir == -1)
                    {
                        Char.getMyChar().cdir = 1;
                    }
                    else if (!Char.getMyChar().isLockMove && !Char.getMyChar().isBlinking)
                    {
                        Char.getMyChar().statusMe = 2;
                        Char.getMyChar().cvx = Char.getMyChar().getSpeed();
                    }
                }
            }
            else if (Char.getMyChar().statusMe == 2)
            {
                GameCanvas.debug("F11", 0);
                if (GameCanvas.keyPressedz[5] || GameCanvas.keyPressedz[20])
                {
                    GameCanvas.keyPressedz[5] = false;
                    GameCanvas.keyPressedz[20] = false;
                    doFire(false);
                }
                else if (GameCanvas.keyHold[Key.NUM2] || GameCanvas.keyHold[24])
                {
                    Char.getMyChar().cvy = ((!Char.getMyChar().canJumpHigh) ? (-8) : (-10));
                    Char.getMyChar().statusMe = 3;
                    Char.getMyChar().cp1 = 0;
                }
                else if (GameCanvas.keyHold[Key.NUM1])
                {
                    Char.getMyChar().cdir = -1;
                    Char.getMyChar().cvy = ((!Char.getMyChar().canJumpHigh) ? (-8) : (-10));
                    Char.getMyChar().cvx = -4;
                    Char.getMyChar().statusMe = 3;
                    Char.getMyChar().cp1 = 0;
                }
                else if (GameCanvas.keyHold[Key.NUM3])
                {
                    Char.getMyChar().cdir = 1;
                    Char.getMyChar().cvy = ((!Char.getMyChar().canJumpHigh) ? (-8) : (-10));
                    Char.getMyChar().cvx = 4;
                    Char.getMyChar().statusMe = 3;
                    Char.getMyChar().cp1 = 0;
                }
                else if (GameCanvas.keyHold[Key.NUM4] || GameCanvas.keyHold[23])
                {
                    if (Char.getMyChar().cdir == 1)
                    {
                        Char.getMyChar().cdir = -1;
                    }
                    else
                    {
                        Char.getMyChar().cvx = -Char.getMyChar().getSpeed() + Char.getMyChar().cBonusSpeed;
                    }
                }
                else if (GameCanvas.keyHold[Key.NUM6] || GameCanvas.keyHold[22])
                {
                    if (Char.getMyChar().cdir == -1)
                    {
                        Char.getMyChar().cdir = 1;
                    }
                    else
                    {
                        Char.getMyChar().cvx = Char.getMyChar().getSpeed() + Char.getMyChar().cBonusSpeed;
                    }
                }
            }
            else if (Char.getMyChar().statusMe == 3)
            {
                GameCanvas.debug("F12", 0);
                if (GameCanvas.keyPressedz[5] || GameCanvas.keyPressedz[20])
                {
                    GameCanvas.keyPressedz[5] = false;
                    GameCanvas.keyPressedz[20] = false;
                    doFire(false);
                }

                if (GameCanvas.keyHold[Key.NUM4] || GameCanvas.keyHold[Key.NUM1] || GameCanvas.keyHold[23])
                {
                    if (Char.getMyChar().cdir == 1)
                    {
                        Char.getMyChar().cdir = -1;
                    }
                    else
                    {
                        Char.getMyChar().cvx = -Char.getMyChar().getSpeed();
                    }
                }
                else if (GameCanvas.keyHold[Key.NUM6] || GameCanvas.keyHold[Key.NUM3] || GameCanvas.keyHold[22])
                {
                    if (Char.getMyChar().cdir == -1)
                    {
                        Char.getMyChar().cdir = 1;
                    }
                    else
                    {
                        Char.getMyChar().cvx = Char.getMyChar().getSpeed();
                    }
                }

                if ((GameCanvas.keyHold[Key.NUM2] || GameCanvas.keyHold[Key.NUM1] || GameCanvas.keyHold[Key.NUM3] || GameCanvas.keyHold[24]) && Char.getMyChar().canJumpHigh && Char.getMyChar().cp1 == 0 && Char.getMyChar().cvy > -4)
                {
                    Char.getMyChar().cp1++;
                    Char.getMyChar().cvy = -7;
                }
            }
            else if (Char.getMyChar().statusMe == 4)
            {
                GameCanvas.debug("F13", 0);
                if (GameCanvas.keyPressedz[5] || GameCanvas.keyPressedz[20])
                {
                    GameCanvas.keyPressedz[5] = false;
                    GameCanvas.keyPressedz[20] = false;
                    doFire(false);
                }

                if (GameCanvas.keyPressedz[2] || GameCanvas.keyHold[24])
                {
                    GameCanvas.clearKeyPressed();
                }

                if (GameCanvas.keyHold[Key.NUM4] || GameCanvas.keyHold[23])
                {
                    if (Char.getMyChar().cdir == 1)
                    {
                        Char.getMyChar().cdir = -1;
                    }
                    else
                    {
                        Char.getMyChar().cvx = -Char.getMyChar().getSpeed();
                    }
                }
                else if (GameCanvas.keyHold[Key.NUM6] || GameCanvas.keyHold[22])
                {
                    if (Char.getMyChar().cdir == -1)
                    {
                        Char.getMyChar().cdir = 1;
                    }
                    else
                    {
                        Char.getMyChar().cvx = Char.getMyChar().getSpeed();
                    }
                }
            }
            else if (Char.getMyChar().statusMe == 10)
            {
                GameCanvas.debug("F14", 0);
                if (GameCanvas.keyPressedz[5] || GameCanvas.keyPressedz[20])
                {
                    GameCanvas.keyPressedz[5] = false;
                    GameCanvas.keyPressedz[20] = false;
                    doFire(false);
                }

                if (GameCanvas.keyHold[Key.NUM2] || GameCanvas.keyHold[24])
                {
                    Char.getMyChar().cvy = -10;
                    Char.getMyChar().statusMe = 3;
                    Char.getMyChar().cp1 = 0;
                }
                else if (GameCanvas.keyHold[Key.NUM4] || GameCanvas.keyHold[23])
                {
                    if (Char.getMyChar().cdir == 1)
                    {
                        Char.getMyChar().cdir = -1;
                    }
                    else
                    {
                        Char.getMyChar().cvx = -5;
                    }
                }
                else if (GameCanvas.keyHold[Key.NUM6] || GameCanvas.keyHold[22])
                {
                    if (Char.getMyChar().cdir == -1)
                    {
                        Char.getMyChar().cdir = 1;
                    }
                    else
                    {
                        Char.getMyChar().cvx = 5;
                    }
                }
            }
            else if (Char.getMyChar().statusMe == 7)
            {
                GameCanvas.debug("F15", 0);
                if (GameCanvas.keyPressedz[5])
                {
                    GameCanvas.keyPressedz[5] = false;
                }

                if (GameCanvas.keyHold[Key.NUM4] || GameCanvas.keyHold[23])
                {
                    if (Char.getMyChar().cdir == 1)
                    {
                        Char.getMyChar().cdir = -1;
                    }
                    else
                    {
                        Char.getMyChar().cvx = -Char.getMyChar().cspeed + 2;
                    }
                }
                else if (GameCanvas.keyHold[Key.NUM6] || GameCanvas.keyHold[22])
                {
                    if (Char.getMyChar().cdir == -1)
                    {
                        Char.getMyChar().cdir = 1;
                    }
                    else
                    {
                        Char.getMyChar().cvx = Char.getMyChar().cspeed - 2;
                    }
                }
            }
            else if (Char.getMyChar().statusMe == 11)
            {
                GameCanvas.debug("F16", 0);
                if (GameCanvas.keyPressedz[5] || GameCanvas.keyPressedz[20])
                {
                    GameCanvas.keyPressedz[5] = false;
                    GameCanvas.keyPressedz[20] = false;
                    doFire(false);
                }

                if (GameCanvas.keyHold[Key.NUM2] || GameCanvas.keyHold[24])
                {
                    Char.getMyChar().cvy = -10;
                    Char.getMyChar().statusMe = 3;
                    Char.getMyChar().cp1 = 0;
                }
            }
        }
        else
        {
            updateKeyCaptcha();
        }

        GameCanvas.debug("F17", 0);
        if (Main.isPC)
        {
            if (GameCanvas.keyPressedz[(!GameCanvas.isChangMapKey) ? 21 : 2])
            {
                doChangeSkill();
            }
        }
        else if (GameCanvas.keyPressedz[8] && GameCanvas.keyAsciiPress != 56)
        {
            GameCanvas.keyPressedz[8] = false;
            doChangeSkill();
        }

        GameCanvas.debug("F20", 0);
        if (GameCanvas.keyAsciiPress != 0)
        {
            if (!Main.isPC)
            {
                if (TField.isQwerty)
                {
                    if (GameCanvas.keyAsciiPress == 113)
                    {
                        isShortcut = true;
                        if (keySkill[0] != null)
                        {
                            doUseSkill(keySkill[0], true, true);
                        }
                    }
                    else if (GameCanvas.keyAsciiPress == 119)
                    {
                        isShortcut = true;
                        if (keySkill[1] != null)
                        {
                            doUseSkill(keySkill[1], true, true);
                        }
                    }
                    else if (GameCanvas.keyAsciiPress == 101)
                    {
                        isShortcut = true;
                        if (keySkill[2] != null)
                        {
                            doUseSkill(keySkill[2], true, true);
                        }
                    }
                }
                else if (!GameCanvas.isMoveNumberPad)
                {
                // typing used to open the chat box; there is none any more
                }
                else if (GameCanvas.keyAsciiPress == 55)
                {
                    isShortcut = true;
                    if (keySkill[0] != null)
                    {
                        doUseSkill(keySkill[0], true, true);
                    }
                }
                else if (GameCanvas.keyAsciiPress == 56)
                {
                    isShortcut = true;
                    if (keySkill[1] != null)
                    {
                        doUseSkill(keySkill[1], true, true);
                    }
                }
                else if (GameCanvas.keyAsciiPress == 57)
                {
                    isShortcut = true;
                    if (keySkill[2] != null)
                    {
                        doUseSkill(keySkill[2], true, true);
                    }
                }
            }

            GameCanvas.keyAsciiPress = 0;
        }

        GameCanvas.clearKeyPressed();
        GameCanvas.debug("F23", 0);
    }

    public void resetKeyNotRelease()
    {
        if (GameCanvas.timePutKeyPc > 0)
        {
            GameCanvas.timePutKeyPc--;
        }

        if (GameCanvas.timePutKeyPc <= 0)
        {
            GameCanvas.timePutKeyPc = 0;
            if (Main.isPC && (Char.getMyChar().statusMe == 2 || Char.getMyChar().statusMe == 3 || Char.getMyChar().statusMe == 4))
            {
                bool[] keyHold = GameCanvas.keyHold;
                int nUM = Key.NUM2;
                bool[] keyHold2 = GameCanvas.keyHold;
                int nUM2 = Key.NUM4;
                bool[] keyHold3 = GameCanvas.keyHold;
                int nUM3 = Key.NUM6;
                bool[] keyHold4 = GameCanvas.keyHold;
                bool flag;
                GameCanvas.keyHold[23] = (flag = (GameCanvas.keyHold[24] = false));
                keyHold4[22] = (flag = flag);
                keyHold3[nUM3] = (flag = flag);
                keyHold2[nUM2] = (flag = flag);
                keyHold[nUM] = flag;
            }
        }
    }

    private void updateKeyTouchControl()
    {
        bool flag = false;
        mScreen.keyTouch = -1;
        if (GameCanvas.isPointerHoldIn(TileMap.posMiniMapX, TileMap.posMiniMapY, TileMap.wMiniMap, TileMap.hMiniMap) && GameCanvas.isPointerClick && GameCanvas.isPointerJustRelease)
        {
            doShowMap();
            flag = true;
        }

        if (!GameCanvas.isTouch || (GameCanvas.menu.showMenu && GameCanvas.isTouchControlSmallScreen) || GameCanvas.currentDialog != null || ChatPopup.currentMultilineChatPopup != null || GameCanvas.menu.showMenu || isPaintPopup())
        {
            return;
        }

        if (GameCanvas.isPointerHoldIn(xC, yC, chatIconSize, chatIconSize))
        {
            mScreen.keyTouch = 15;
            if (GameCanvas.isPointerClick && GameCanvas.isPointerJustRelease)
            {
                Sound.play(Sound.MBClick, 0.5f);
                // Offline: the original chat icon opens the dev terminal in the message window.
                openTerminal();
                GameCanvas.isPointerJustRelease = false;
                GameCanvas.isPointerClick = false;
                flag = true;
            }
        }

        if (!Char.getMyChar().isCaptcha)
        {
            if (isNotPaintTouchControl())
            {
                return;
            }

            if (!isTouchKey)
            {
                gamePad.update();
            }
            else if (!Main.isPC)
            {
                if (GameCanvas.isPointerHoldIn(xU, yU, 34, 34))
                {
                    mScreen.keyTouch = 3;
                    GameCanvas.keyHold[2] = true;
                    resetAuto();
                    flag = true;
                }
                else if (GameCanvas.isPointerDown)
                {
                    GameCanvas.keyHold[2] = false;
                }

                if (GameCanvas.isPointerHoldIn(xU - 30, yU, 30, 34))
                {
                    GameCanvas.keyHold[1] = true;
                    resetAuto();
                    flag = true;
                }
                else if (GameCanvas.isPointerDown)
                {
                    GameCanvas.keyHold[1] = false;
                }

                if (GameCanvas.isPointerHoldIn(xU + 34, yU, 30, 34))
                {
                    GameCanvas.keyHold[3] = true;
                    resetAuto();
                    flag = true;
                }
                else if (GameCanvas.isPointerDown)
                {
                    GameCanvas.keyHold[3] = false;
                }

                if (GameCanvas.isPointerHoldIn(xL, yL, 34, 34))
                {
                    mScreen.keyTouch = 4;
                    GameCanvas.keyHold[4] = true;
                    resetAuto();
                    flag = true;
                }
                else if (GameCanvas.isPointerDown)
                {
                    GameCanvas.keyHold[4] = false;
                }

                if (GameCanvas.isPointerHoldIn(xR - 5, yR, 40, 34))
                {
                    mScreen.keyTouch = 6;
                    GameCanvas.keyHold[6] = true;
                    resetAuto();
                    flag = true;
                }
                else if (GameCanvas.isPointerDown)
                {
                    GameCanvas.keyHold[6] = false;
                }
            }

            if (!Main.isPC && GameCanvas.isPointerHoldIn(xF, yF, 54, 54))
            {
                GameCanvas.keyHold[5] = true;
                mScreen.keyTouch = 5;
                if (GameCanvas.isPointerJustRelease)
                {
                    GameCanvas.keyPressedz[5] = true;
                    flag = true;
                }
            }
        }
        else
        {
            updateTouchCaptcha();
        }

        if (Char.getMyChar().ctaskId > 1)
        {
            if (GameCanvas.isPointerHoldIn(xMP, yMP, 34, 34))
            {
                mScreen.keyTouch = 11;
                if (GameCanvas.isPointerClick && GameCanvas.isPointerJustRelease)
                {
                    doUseMP();
                    Sound.play(Sound.MBClick, 0.5f);
                    GameCanvas.isPointerClick = false;
                    flag = true;
                }
            }

            if (GameCanvas.isPointerHoldIn(xHP, yHP, 34, 34))
            {
                mScreen.keyTouch = 10;
                if (GameCanvas.isPointerClick && GameCanvas.isPointerJustRelease)
                {
                    doUseHP();
                    Sound.play(Sound.MBClick, 0.5f);
                    GameCanvas.isPointerClick = false;
                    flag = true;
                }
            }

            if (!Main.isPC && GameCanvas.isPointerHoldIn(xTG, yTG, 34, 34))
            {
                mScreen.keyTouch = 13;
                if (GameCanvas.isPointerClick && GameCanvas.isPointerJustRelease)
                {
                    Char.getMyChar().findNextFocusByKey();
                    Sound.play(Sound.MBClick, 0.5f);
                    flag = true;
                }
            }
        }

        if (hasBookSkill() && (GameCanvas.isPointerHoldIn(xSkill + xS[0], yS[0], onScreenSkill.Length * 30, 30) || (!GameCanvas.isTouchControl && GameCanvas.isPointerHoldIn(xSkill + xS[0], yS[0], 30, onScreenSkill.Length * 25))) && GameCanvas.isPointerClick && GameCanvas.isPointerJustRelease)
        {
            int num = 0;
            num = (GameCanvas.isTouchControl ? ((GameCanvas.pxLast - (xSkill + xS[0])) / 30) : ((GameCanvas.pyLast - (ySkill + yS[0])) / 25));
            selectedIndexSkill = num;
            if (indexSelect < 0)
            {
                indexSelect = 0;
            }

            if (selectedIndexSkill > onScreenSkill.Length - 1)
            {
                selectedIndexSkill = onScreenSkill.Length - 1;
            }

            flag = true;
            Skill skill = onScreenSkill[selectedIndexSkill];
            doUseSkill(skill, false, true);
            isShortcut = true;
        }

        if (Main.isPC)
        {
            if (GameCanvas.keyPressedz[(!GameCanvas.isChangMapKey) ? 1 : 6])
            {
                GameCanvas.keyPressedz[(!GameCanvas.isChangMapKey) ? 1 : 6] = false;
                Skill skill2 = onScreenSkill[0];
                doUseSkill(skill2, false, true);
            }

            if (GameCanvas.keyPressedz[GameCanvas.isChangMapKey ? 7 : 2])
            {
                GameCanvas.keyPressedz[GameCanvas.isChangMapKey ? 7 : 2] = false;
                Skill skill3 = onScreenSkill[1];
                doUseSkill(skill3, false, true);
            }

            if (GameCanvas.keyPressedz[GameCanvas.isChangMapKey ? 8 : 3])
            {
                GameCanvas.keyPressedz[GameCanvas.isChangMapKey ? 8 : 3] = false;
                Skill skill4 = onScreenSkill[2];
                doUseSkill(skill4, false, true);
            }

            if (GameCanvas.keyPressedz[GameCanvas.isChangMapKey ? 9 : 4])
            {
                GameCanvas.keyPressedz[GameCanvas.isChangMapKey ? 9 : 4] = false;
                Skill skill5 = onScreenSkill[3];
                doUseSkill(skill5, false, true);
            }

            if (GameCanvas.keyPressedz[(!GameCanvas.isChangMapKey) ? 27 : 0])
            {
                GameCanvas.keyPressedz[(!GameCanvas.isChangMapKey) ? 27 : 0] = false;
                Skill skill6 = onScreenSkill[4];
                doUseSkill(skill6, false, true);
            }
        }

        if (GameCanvas.isPointerJustRelease)
        {
            bool releasedMovement = GameCanvas.keyHold[Key.NUM4] || GameCanvas.keyHold[Key.NUM6] || GameCanvas.keyHold[22] || GameCanvas.keyHold[23];
            GameCanvas.keyHold[Key.NUM1] = false;
            GameCanvas.keyHold[Key.NUM2] = false;
            GameCanvas.keyHold[Key.NUM3] = false;
            GameCanvas.keyHold[Key.NUM4] = false;
            GameCanvas.keyHold[Key.NUM6] = false;
            GameCanvas.keyHold[22] = false;
            GameCanvas.keyHold[23] = false;
            GameCanvas.keyHold[24] = false;
            GameCanvas.keyHold[25] = false;
            if (releasedMovement)
                Char.getMyChar().StopGroundMovementOnRelease();
        }

        if (!flag)
        {
            doFocusbyTouch();
        }
    }

    public void updateKeyTask()
    {
        if (!isPaintTask)
        {
            return;
        }

        if (indexTitle == 0)
        {
            if (indexTitle == 0 && (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25]))
            {
                indexTitle = 1;
                indexRow = -1;
                scrMain.clear();
                scrInfo.clear();
            }
        }
        else
        {
            if (indexRow < 0)
            {
                indexRow = 0;
            }

            if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
            {
                if (indexRow == 0)
                {
                    indexTitle--;
                    indexRow = -1;
                }
                else
                {
                    indexRow--;
                }

                scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
            }
            else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
            {
                indexRow++;
                if (indexRow >= indexRowMax)
                {
                    indexRow = 0;
                }

                scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
            }
        }

        if (GameCanvas.isTouch)
        {
            ScrollResult scrollResult = scrMain.updateKey();
            if (scrollResult.isDowning || scrollResult.isFinish)
            {
                indexRow = scrollResult.selected;
                indexTitle = 1;
            }
        }
    }

    public void updateKeyClanInfo()
    {
        if (!isPaintClan)
        {
            return;
        }

        if (indexTitle == 0)
        {
            if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
            {
                if (Char.clan == null)
                {
                    indexTitle = 0;
                }
                else
                {
                    indexTitle = 1;
                }

                indexSelect = 0;
                indexRow = -1;
                if (indexMenu == 0)
                {
                    indexSelect = Char.clan.itemLevel;
                }

                scrMain.clear();
                scrInfo.clear();
            }

            if (GameCanvas.keyPressedz[Key.NUM4] || GameCanvas.keyPressedz[23])
            {
                indexSelect = 0;
                indexRow = -1;
                indexMenu--;
                scrMain.clear();
                scrInfo.clear();
                if (indexMenu < 0)
                {
                    indexMenu = mResources.CLAN.Length - 1;
                }

                if (indexMenu >= mResources.CLAN.Length)
                {
                    indexMenu = 0;
                }

                if (indexMenu == 1 && isRequestMember)
                {
                    Service.gI().requestClanMember();
                    isRequestMember = false;
                }
                else if (indexMenu == 2)
                {
                    Service.gI().requestClanItem();
                }
                else if (indexMenu == 3)
                {
                    Service.gI().requestClanLog();
                }
                else if (indexMenu == 4)
                {
                    Service.gI().requestClanItem();
                }

                setPopupSize(175, 200);
            }

            if (GameCanvas.keyPressedz[Key.NUM6] || GameCanvas.keyPressedz[22])
            {
                indexSelect = 0;
                indexRow = -1;
                indexMenu++;
                scrMain.clear();
                scrInfo.clear();
                if (indexMenu < 0)
                {
                    indexMenu = mResources.CLAN.Length - 1;
                }

                if (indexMenu >= mResources.CLAN.Length)
                {
                    indexMenu = 0;
                }

                if (indexMenu == 1 && isRequestMember)
                {
                    Service.gI().requestClanMember();
                    isRequestMember = false;
                }
                else if (indexMenu == 2 && Char.clan.items == null)
                {
                    Service.gI().requestClanItem();
                }
                else if (indexMenu == 3)
                {
                    Service.gI().requestClanLog();
                }
                else if (indexMenu == 4)
                {
                    Service.gI().requestClanItem();
                }

                setPopupSize(175, 200);
            }

            setClanComand();
        }
        else if (isPaintItemInfo)
        {
            if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
            {
                indexRow--;
                if (indexRow < 0)
                {
                    indexRow = indexRowMax - 1;
                }

                scrInfo.moveTo(indexRow * scrInfo.ITEM_SIZE);
            }
            else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
            {
                indexRow++;
                if (indexRow >= indexRowMax)
                {
                    indexRow = 0;
                }

                scrInfo.moveTo(indexRow * scrInfo.ITEM_SIZE);
            }
        }
        else
        {
            if (indexRow < 0)
            {
                indexRow = 0;
            }

            if (indexMenu == 2)
            {
                if (Char.clan.items != null && Char.clan.items != null)
                {
                    if (GameCanvas.keyPressedz[Key.NUM4] || GameCanvas.keyPressedz[23])
                    {
                        indexSelect--;
                        if (indexSelect < 0)
                        {
                            indexSelect = Char.clan.items.Length - 1;
                        }
                    }
                    else if (GameCanvas.keyPressedz[Key.NUM6] || GameCanvas.keyPressedz[22])
                    {
                        indexSelect++;
                        if (indexSelect >= Char.clan.items.Length)
                        {
                            indexSelect = 0;
                        }
                    }
                    else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
                    {
                        if (indexSelect + columns <= Char.clan.items.Length - 1)
                        {
                            indexSelect += columns;
                        }
                    }
                    else if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
                    {
                        if (indexSelect >= 0 && indexSelect < columns)
                        {
                            indexTitle = 0;
                            indexSelect = 0;
                        }
                        else if (indexSelect - columns >= 0)
                        {
                            indexSelect -= columns;
                        }
                    }

                    scrMain.moveTo(indexSelect / columns * scrMain.ITEM_SIZE);
                }
            }
            else if (indexMenu == 0 && indexTitle == 1)
            {
                if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
                {
                    indexTitle++;
                }
                else if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
                {
                    indexTitle--;
                }
            }
            else if (indexMenu == 4)
            {
                updateKeyThanThu();
            }
            else if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
            {
                if (indexRow == 0)
                {
                    indexTitle--;
                    indexRow = -1;
                }
                else
                {
                    indexRow--;
                }

                scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
                if (indexMenu == 1 && isRequestMember)
                {
                    Service.gI().requestClanMember();
                    isRequestMember = false;
                }
            }
            else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
            {
                indexRow++;
                if (indexRow >= indexRowMax)
                {
                    indexRow = 0;
                }

                scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
            }

            setClanComand();
        }

        if (GameCanvas.isTouch)
        {
            updateKeyClanInfoTouch();
        }

        GameCanvas.clearKeyHold();
        GameCanvas.clearKeyPressed();
    }

    private void updateKeyClanInfoTouch()
    {
        if (GameCanvas.currentDialog != null || GameCanvas.menu.showMenu)
        {
            return;
        }

        if (GameCanvas.isPointerJustRelease)
        {
            if (GameCanvas.isPointerHoldIn(popupX, popupY, popupW, Hitem) && (!isPaintItemInfo || GameCanvas.w >= 320) && GameCanvas.isPointerClick)
            {
                if (GameCanvas.isPointerHoldIn(gW2 - 90, popupY + 5, 60, 40))
                {
                    indexSelect = 0;
                    indexMenu--;
                    indexRow = 0;
                    Sound.play(Sound.MBClick, 0.5f);
                }

                if (GameCanvas.isPointerHoldIn(gW2 + 20, popupY + 5, 60, 40))
                {
                    indexSelect = 0;
                    indexMenu++;
                    indexRow = 0;
                    Sound.play(Sound.MBClick, 0.5f);
                }

                isPaintItemInfo = false;
                scrMain.clear();
                scrInfo.clear();
                if (indexMenu < 0)
                {
                    indexMenu = mResources.CLAN.Length - 1;
                }

                if (indexMenu > mResources.CLAN.Length - 1)
                {
                    indexMenu = 0;
                }

                indexTitle = 1;
                if (indexMenu == 1 && isRequestMember)
                {
                    Service.gI().requestClanMember();
                    isRequestMember = false;
                }
                else if (indexMenu == 2 && Char.clan != null && Char.clan.items == null)
                {
                    Service.gI().requestClanItem();
                }

                if (indexMenu == 3)
                {
                    Service.gI().requestClanLog();
                }

                if (indexMenu == 4)
                {
                    Service.gI().requestClanItem();
                }

                setPopupSize(175, 200);
                setClanComand();
            }

            if (indexMenu == 4)
            {
                updateTouchThanThu();
            }
        }

        if (isPaintItemInfo)
        {
            ScrollResult scrollResult = scrInfo.updateKey();
            if (scrollResult.isDowning || scrollResult.isFinish)
            {
                indexRow = scrollResult.selected;
                indexTitle = 1;
            }

            if (GameCanvas.isTouchControlSmallScreen)
            {
                return;
            }
        }

        if (indexMenu == 2)
        {
            ScrollResult scrollResult2 = scrMain.updateKey();
            if (scrollResult2.isDowning || scrollResult2.isFinish)
            {
                indexSelect = scrollResult2.selected;
                indexTitle = 1;
                actionPerform(1509, null);
            }
        }
        else if (indexMenu == 0 && GameCanvas.isPointerHoldIn(popupX + 18, popupY + 32, 5 * indexSize, indexSize) && GameCanvas.isPointerJustRelease && GameCanvas.isPointerClick)
        {
            if (Char.clan != null)
            {
                indexSelect = Char.clan.itemLevel;
                indexTitle = 1;
            }
        }
        else if (indexMenu == 0 || indexMenu == 3)
        {
            if (isPaintItemInfo)
            {
                return;
            }

            ScrollResult scrollResult3 = scrMain.updateKey();
            if (scrollResult3.isDowning || scrollResult3.isFinish)
            {
                indexRow = scrollResult3.selected;
                indexTitle = ((indexMenu != 0) ? 1 : 2);
                if (scrollResult3.isFinish)
                {
                    scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
                }
            }
        }
        else if (indexMenu == 1 && vClan.size() != 0)
        {
            ScrollResult scrollResult4 = scrMain.updateKey();
            if (scrollResult4.isDowning || scrollResult4.isFinish)
            {
                indexRow = scrollResult4.selected;
                updateCommandForUI();
            }
        }
    }

    // Shared grid policy for bag, equipment, mount and bijuu equipment. Detail panes never own arrows.
    private void NavigateInfoItemGrid()
    {
        TabGridNavigation.Direction direction;
        if (!TryGridDirection(out direction))
            return;
        int count = indexMenu == 0 ? Char.getMyChar().arrItemBag.Length : 5;
        int next = indexMenu == 4 ? TabGridNavigation.MoveEquipment(indexSelect, direction) : TabGridNavigation.Move(indexSelect, count, indexMenu == 0 ? columns : 1, direction);
        if (next == TabGridNavigation.Title)
        {
            TrangBiBackToTitle();
            return;
        }

        indexSelect = next;
        left = (center = null);
        setLCR();
        if (indexMenu == 0)
        {
            BagInfoFollow();
            scrMain.moveTo(indexSelect / columns * scrMain.ITEM_SIZE);
        }
    }

    // Crafting has an additional equipment row above materials. Resolve exactly one transition
    // and consume the direction before refreshing commands, so title focus cannot fall through.
    private bool NavigateCraftItemGrid()
    {
        if ((!isPaintUpGrade && !isPaintBox) || indexTitle < 1)
            return false;
        TabGridNavigation.Direction direction;
        if (!TryGridDirection(out direction))
            return false;
        bool equipmentRow = isPaintUpGrade && indexMenu == 0 && indexTitle == 1;
        int count = equipmentRow ? 2 : isPaintUpGrade && indexMenu == 0 ? (arrItemUpGrade == null ? 0 : arrItemUpGrade.Length) : isPaintBox && indexMenu == 0 ? (Char.getMyChar().arrItemBox == null ? 0 : Char.getMyChar().arrItemBox.Length) : Char.getMyChar().arrItemBag.Length;
        int next = TabGridNavigation.Move(indexSelect, count, equipmentRow ? 2 : columns, direction);
        if (equipmentRow && direction == TabGridNavigation.Direction.Down)
        {
            indexTitle = 2;
            indexSelect = 0;
        }
        else if (next == TabGridNavigation.Title)
        {
            indexTitle = isPaintUpGrade && indexMenu == 0 && !equipmentRow && direction == TabGridNavigation.Direction.Up ? 1 : 0;
            indexSelect = 0;
            isPaintItemInfo = false;
            scrInfo.clear();
        }
        else
            indexSelect = next;
        GameCanvas.clearKeyHold();
        GameCanvas.clearKeyPressed();
        left = (center = null);
        if (indexTitle == 0)
            updateKeyBuyItemUITitle();
        else
            updateCommandForUI();
        if (indexTitle > 0 && !equipmentRow)
            scrMain.moveTo(indexSelect / columns * scrMain.ITEM_SIZE);
        return true;
    }

    public void updateKeyUIInforMe()
    {
        if (!isPaintInfoMe || indexMenu == -1 || GameCanvas.currentDialog != null || (isselectedRow >= 1 && indexMenu == 1))
        {
            return;
        }

        if (indexTitle == 0)
        {
            left = (center = null);
            if (indexMenu == 0)
            {
                left = new Command(mResources.SORT, 110221);
            }

            if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
            {
                indexTitle = 1;
                indexSelect = 0;
                indexRow = 0;
                scrMain.clear();
                scrInfo.clear();
                setLCR();
            }

            if (GameCanvas.keyPressedz[Key.NUM4] || GameCanvas.keyPressedz[23])
            {
                indexSelect = 0;
                indexRow = -1;
                indexMenu--;
                scrMain.clear();
                scrInfo.clear();
                if (currentCharViewInfo.charID != Char.getMyChar().charID)
                {
                    if (indexMenu < 3)
                    {
                        indexMenu = 5;
                    }
                }
                else if (indexMenu < 0)
                {
                    indexMenu = mResources.MENUME.Length - 1;
                }

                doMiniMenuInforMe();
            }

            if (GameCanvas.keyPressedz[Key.NUM6] || GameCanvas.keyPressedz[22])
            {
                indexSelect = 0;
                indexRow = -1;
                indexMenu++;
                scrMain.clear();
                scrInfo.clear();
                if (currentCharViewInfo.charID != Char.getMyChar().charID)
                {
                    if (indexMenu > 5)
                    {
                        indexMenu = 3;
                    }
                }
                else if (indexMenu > mResources.MENUME.Length - 1)
                {
                    indexMenu = 0;
                }

                doMiniMenuInforMe();
            }

            setLCR();
        }
        // Offline addition (user request): on large screens setLCR shows the focused item's info
        // automatically, so this branch swallowed left/right and the selection could not move.
        // Left/right now fall through to every tab's own navigation; up/down still scroll the info.
        else if (isPaintItemInfo && !InfoArrowsNavigateSelection && !(GameCanvas.keyPressedz[Key.NUM4] || GameCanvas.keyPressedz[23] || GameCanvas.keyPressedz[Key.NUM6] || GameCanvas.keyPressedz[22]))
        {
            // Offline change (user request): moveTo centres the row, so the first and last presses were
            // clamped and the info did not move until several presses later. Each press now scrolls the
            // info by exactly one line (no wrap-around).
            if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
            {
                scrInfo.ScrollBy(-scrInfo.ITEM_SIZE);
            }
            else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
            {
                scrInfo.ScrollBy(scrInfo.ITEM_SIZE);
            }
        }
        else if (indexMenu == 0)
        {
            NavigateInfoItemGrid();
        }
        else if (indexMenu == 1)
        {
            if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
            {
                if (indexTitle == 1 && indexRow == -1)
                {
                    indexTitle--;
                }
                else if (indexTitle == 1 && indexRow >= 0)
                {
                    indexRow--;
                }

                scrInfo.moveTo(indexRow * scrInfo.ITEM_SIZE);
            }
            else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
            {
                if (indexTitle == 0)
                {
                    indexTitle++;
                }
                else if (indexTitle == 1)
                {
                    indexRow++;
                    if (indexRow >= indexRowMax)
                    {
                        indexRow = 0;
                    }

                    scrInfo.moveTo(indexRow * scrInfo.ITEM_SIZE);
                }

                left = (center = null);
                setLCR();
            }
            else if (GameCanvas.keyPressedz[Key.NUM4] || GameCanvas.keyPressedz[23])
            {
                indexRow = -1;
                if (indexTitle == 1)
                {
                    indexSelect--;
                    if (indexSelect < 0)
                    {
                        TrangBiBackToTitle(); // offline (user request): was wrap to the last skill
                    }
                }

                left = (center = null);
                setLCR();
                scrMain.moveTo(indexSelect * scrMain.ITEM_SIZE);
                scrInfo.clear();
                indexRow = 0;
            }
            else if (GameCanvas.keyPressedz[Key.NUM6] || GameCanvas.keyPressedz[22])
            {
                indexRow = -1;
                if (indexTitle == 1)
                {
                    indexSelect++;
                    if (indexSelect >= Char.getMyChar().nClass.skillTemplates.Length)
                    {
                        TrangBiBackToTitle(); // offline (user request): was wrap to the first skill
                    }
                }

                left = (center = null);
                setLCR();
                scrMain.moveTo(indexSelect * scrMain.ITEM_SIZE);
                scrInfo.clear();
                indexRow = 0;
            }
        }
        else if (indexMenu == 2)
        {
            if (GameCanvas.keyPressedz[Key.NUM4] || GameCanvas.keyPressedz[23] || GameCanvas.keyPressedz[Key.NUM6] || GameCanvas.keyPressedz[22])
            {
                TrangBiBackToTitle(); // offline (user request): left/right return to the tab title
            }
            else if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
            {
                indexTitle--;
            }
            else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
            {
                indexTitle++;
                if (indexTitle >= 5)
                {
                    indexTitle = 1;
                }

                left = (center = null);
                setLCR();
            }
        }
        else if (indexMenu == 3)
        {
            if (indexRow < 0)
            {
                indexRow = 0;
            }

            if (GameCanvas.keyPressedz[Key.NUM4] || GameCanvas.keyPressedz[23] || GameCanvas.keyPressedz[Key.NUM6] || GameCanvas.keyPressedz[22])
            {
                TrangBiBackToTitle(); // offline (user request): left/right return to the tab title
            }
            else if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
            {
                if (indexRow == 0)
                {
                    indexTitle--;
                    indexRow = -1;
                }
                else
                {
                    indexRow--;
                }

                scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
            }
            else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
            {
                indexRow++;
                if (indexRow >= indexRowMax)
                {
                    indexRow = 0;
                }

                scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
            }
        }
        else if (indexMenu == 4)
        {
            NavigateInfoItemGrid();
        }
        else if (indexMenu == 5)
        {
            NavigateInfoItemGrid();
        }
        else if (indexMenu == 6)
        {
            if (indexBijuuMenu == 0)
            {
                NavigateInfoItemGrid();
            }
            else if (indexBijuuMenu == 1)
            {
                if (GameCanvas.keyPressedz[Key.NUM4] || GameCanvas.keyPressedz[23] || GameCanvas.keyPressedz[Key.NUM6] || GameCanvas.keyPressedz[22])
                {
                    TrangBiBackToTitle(); // offline (user request): left/right return to the tab title
                }
                else if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
                {
                    indexTitle--;
                }
                else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
                {
                    indexTitle++;
                    if (indexTitle >= 5)
                    {
                        indexTitle = 1;
                    }

                    left = (center = null);
                    setLCR();
                }
            }
            else if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
            {
                if (indexTitle == 1 && indexRow == -1)
                {
                    indexTitle--;
                }
                else if (indexTitle == 1 && indexRow >= 0)
                {
                    indexRow--;
                }

                scrInfo.moveTo(indexRow * scrInfo.ITEM_SIZE);
            }
            else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
            {
                if (indexTitle == 0)
                {
                    indexTitle++;
                }
                else if (indexTitle == 1)
                {
                    indexRow++;
                    if (indexRow >= indexRowMax)
                    {
                        indexRow = 0;
                    }

                    scrInfo.moveTo(indexRow * scrInfo.ITEM_SIZE);
                }

                left = (center = null);
                setLCR();
            }
            else if (GameCanvas.keyPressedz[Key.NUM4] || GameCanvas.keyPressedz[23])
            {
                indexRow = -1;
                if (indexTitle == 1)
                {
                    indexSelect--;
                    if (indexSelect < 0)
                    {
                        TrangBiBackToTitle(); // offline (user request): was wrap to the last skill
                    }
                }

                left = (center = null);
                setLCR();
                scrMain.moveTo(indexSelect * scrMain.ITEM_SIZE);
                scrInfo.clear();
                indexRow = 0;
            }
            else if (GameCanvas.keyPressedz[Key.NUM6] || GameCanvas.keyPressedz[22])
            {
                indexRow = -1;
                if (indexTitle == 1)
                {
                    indexSelect++;
                    if (indexSelect >= Char.getMyChar().nClass.skillTemplates.Length)
                    {
                        TrangBiBackToTitle(); // offline (user request): was wrap to the first skill
                    }
                }

                left = (center = null);
                setLCR();
                scrMain.moveTo(indexSelect * scrMain.ITEM_SIZE);
                scrInfo.clear();
                indexRow = 0;
            }
        }

        if (indexTitle == 0 && indexMenu == 6)
        {
            isPaintItemInfo = false;
            left = (center = null);
            setLCR();
        }

        if (GameCanvas.isTouch)
        {
            updateKeyUIInforMeTouch();
        }

        GameCanvas.clearKeyHold();
        GameCanvas.clearKeyPressed();
    }

    private void updateKeyUIInforMeTouch()
    {
        if (GameCanvas.menu.showMenu || GameCanvas.currentDialog != null)
        {
            return;
        }

        if (GameCanvas.isPointerJustRelease && GameCanvas.isPointerHoldIn(popupX, popupY, popupW, Hitem) && (!isPaintItemInfo || GameCanvas.w >= 320) && GameCanvas.isPointerClick)
        {
            if (GameCanvas.isPointerHoldIn(gW2 - 90, popupY + 5, 60, 40))
            {
                indexSelect = 0;
                indexMenu--;
                Sound.play(Sound.MBClick, 0.5f);
            }

            if (GameCanvas.isPointerHoldIn(gW2 + 20, popupY + 5, 60, 40))
            {
                indexSelect = 0;
                indexMenu++;
                Sound.play(Sound.MBClick, 0.5f);
            }

            isPaintItemInfo = false;
            scrMain.clear();
            scrInfo.clear();
            if (currentCharViewInfo.charID != Char.getMyChar().charID)
            {
                if (indexMenu < 3)
                {
                    indexMenu = mResources.MENUME.Length - 1;
                }

                if (indexMenu > mResources.MENUME.Length - 1)
                {
                    indexMenu = 3;
                }
            }
            else
            {
                if (indexMenu < 0)
                {
                    indexMenu = mResources.MENUME.Length - 1;
                }

                if (indexMenu > mResources.MENUME.Length - 1)
                {
                    indexMenu = 0;
                }
            }

            indexTitle = 1;
            indexSelect = -1;
            doMiniMenuInforMe();
        }

        if (isPaintItemInfo)
        {
            ScrollResult scrollResult = scrInfo.updateKey();
            if (scrollResult.isDowning || scrollResult.isFinish)
            {
                indexRow = scrollResult.selected;
                indexTitle = 1;
            }

            if (GameCanvas.isTouchControlSmallScreen)
            {
                return;
            }
        }

        if (indexMenu == 0)
        {
            ScrollResult scrollResult2 = scrMain.updateKey();
            if (!scrollResult2.isDowning && !scrollResult2.isFinish)
            {
                return;
            }

            if (indexSelect != scrollResult2.selected)
            {
                indexSelect = scrollResult2.selected;
                left = (center = null);
                if (GameCanvas.isTouchControlSmallScreen)
                {
                    setLCR();
                }
                else
                {
                    Item item = getItemFocus(3);
                    if (item != null)
                    {
                        Sound.play(Sound.MBClick, 0.5f);
                        actBagViewItemInfo();
                    }
                    else
                    {
                        isPaintItemInfo = false;
                        left = cmdBagSortItem;
                    }
                }
            }

            indexTitle = 1;
        }
        else if (indexMenu == 1)
        {
            ScrollResult scrollResult3 = scrMain.updateKey();
            if (scrollResult3.isDowning || scrollResult3.isFinish)
            {
                if (indexSelect != scrollResult3.selected)
                {
                    indexSelect = scrollResult3.selected;
                    if (indexSelect >= Char.getMyChar().nClass.skillTemplates.Length)
                    {
                        indexSelect = -1;
                    }

                    left = (center = null);
                    setLCR();
                    scrInfo.clear();
                    indexRow = 0;
                    if (indexSelect > -1)
                    {
                        Sound.play(Sound.MBClick, 0.5f);
                    }
                }

                indexTitle = 1;
            }
            else
            {
                ScrollResult scrollResult4 = scrInfo.updateKey();
                if ((scrollResult4.isDowning || scrollResult4.isFinish) && indexRow != scrollResult4.selected)
                {
                    indexRow = scrollResult4.selected;
                }
            }
        }
        else if (indexMenu == 2)
        {
            if (!GameCanvas.isPointerJustRelease || !GameCanvas.isPointerJustRelease)
            {
                return;
            }

            int num = 0;
            if (!Main.isPC)
            {
                num = indexSize;
            }

            if (GameCanvas.isPointerHoldIn(popupX + 5, popupY + 52, popupW - 10, 111 - num) && GameCanvas.isPointerClick)
            {
                int num2 = (GameCanvas.py - (popupY + 52)) / ((!Main.isPC) ? 22 : 27);
                num2++;
                if (num2 == indexTiemNang)
                {
                    doUPPOINT();
                }

                indexTitle = num2;
                indexTiemNang = num2;
                Sound.play(Sound.MBClick, 0.5f);
                setLCR();
            }
        }
        else if (indexMenu == 3)
        {
            ScrollResult scrollResult5 = scrMain.updateKey();
            if (scrollResult5.isDowning || scrollResult5.isFinish)
            {
                indexRow = scrollResult5.selected;
                indexTitle = 1;
            }
        }
        else if (indexMenu == 4)
        {
            if (GameCanvas.isPointerJustRelease)
            {
                indexTitle = 1;
                if (GameCanvas.isPointerHoldIn(popupX + 4, popupY + 35, indexSize, 130))
                {
                    int num3 = (GameCanvas.py - (popupY + 35)) / indexSize;
                    num3 *= 2;
                    indexSelect = num3;
                    Sound.play(Sound.MBClick, 0.5f);
                    left = (center = null);
                    setLCR();
                }

                if (GameCanvas.isPointerHoldIn(popupX + popupW - 30, popupY + 35, indexSize, 130))
                {
                    int num4 = (GameCanvas.pyLast - (popupY + 35)) / indexSize;
                    num4 = num4 * 2 + 1;
                    indexSelect = num4;
                    left = (center = null);
                    setLCR();
                    Sound.play(Sound.MBClick, 0.5f);
                }

                if (GameCanvas.isPointerHoldIn(popupX + 4, popupY + 165, popupW - 8, indexSize))
                {
                    int num5 = (GameCanvas.pxLast - (popupX + 4)) / indexSize;
                    num5 = 10 + num5;
                    indexSelect = num5;
                    left = (center = null);
                    setLCR();
                    Sound.play(Sound.MBClick, 0.5f);
                }
            }
        }
        else if (indexMenu == 5)
        {
            if (!GameCanvas.isPointerJustRelease)
            {
                return;
            }

            for (int i = 0; i < xMounts.Length; i++)
            {
                if (i == 4)
                {
                    if (GameCanvas.isPointerHoldIn(xMounts[i], yMounts[i], 84, 75) && GameCanvas.isPointerClick)
                    {
                        indexTitle = 1;
                        indexSelect = 4;
                        setLCR();
                        if (!GameCanvas.isTouchControlSmallScreen && center != null)
                        {
                            actionPerform(center.idAction, center.p);
                        }
                    }
                }
                else
                {
                    if (!GameCanvas.isPointerHoldIn(xMounts[i], yMounts[i], indexSize, indexSize) || !GameCanvas.isPointerClick)
                    {
                        continue;
                    }

                    indexTitle = 1;
                    indexSelect = i;
                    setLCR();
                    if (!GameCanvas.isTouchControlSmallScreen)
                    {
                        if (currentCharViewInfo.arrItemMounts[indexSelect] != null)
                        {
                            actionPerform(center.idAction, center.p);
                        }
                        else
                        {
                            isPaintItemInfo = false;
                        }
                    }
                }
            }
        }
        else
        {
            if (indexMenu != 6)
            {
                return;
            }

            if (indexBijuuMenu == 0)
            {
                if (!GameCanvas.isPointerJustRelease)
                {
                    return;
                }

                for (int j = 0; j < xMounts.Length; j++)
                {
                    if (j == 4)
                    {
                        if (GameCanvas.isPointerHoldIn(xMounts[j], yMounts[j], 84, 75) && GameCanvas.isPointerClick)
                        {
                            indexTitle = 1;
                            indexSelect = 4;
                            setLCR();
                            if (!GameCanvas.isTouchControlSmallScreen && center != null)
                            {
                                actionPerform(center.idAction, center.p);
                            }
                        }
                    }
                    else
                    {
                        if (!GameCanvas.isPointerHoldIn(xMounts[j], yMounts[j], indexSize, indexSize) || !GameCanvas.isPointerClick)
                        {
                            continue;
                        }

                        indexTitle = 1;
                        indexSelect = j;
                        setLCR();
                        if (!GameCanvas.isTouchControlSmallScreen)
                        {
                            if (currentCharViewInfo.arrItemViThu[indexSelect] != null)
                            {
                                actionPerform(center.idAction, center.p);
                            }
                            else
                            {
                                isPaintItemInfo = false;
                            }
                        }
                    }
                }

                return;
            }

            if (indexBijuuMenu == 1)
            {
                if (GameCanvas.isPointerJustRelease && GameCanvas.isPointerHoldIn(popupX + 5, popupY + 52, popupW - 10, 130) && GameCanvas.isPointerClick)
                {
                    int num6 = (GameCanvas.py - (popupY + 52)) / 32;
                    num6++;
                    if (num6 == indexTiemNang)
                    {
                        actBijuuMenuPoint();
                    }

                    indexTitle = num6;
                    indexTiemNang = num6;
                    setLCR();
                }

                return;
            }

            ScrollResult scrollResult6 = scrMain.updateKey();
            if (scrollResult6.isDowning || scrollResult6.isFinish)
            {
                if (indexSelect != scrollResult6.selected)
                {
                    indexSelect = scrollResult6.selected;
                    if (indexSelect >= Char.getMyChar().nClass.skillTemplates.Length)
                    {
                        indexSelect = -1;
                    }

                    left = (center = null);
                    setLCR();
                    scrInfo.clear();
                    indexRow = 0;
                }

                indexTitle = 1;
            }
            else
            {
                ScrollResult scrollResult7 = scrInfo.updateKey();
                if ((scrollResult7.isDowning || scrollResult7.isFinish) && indexRow != scrollResult7.selected)
                {
                    indexRow = scrollResult7.selected;
                }
            }
        }
    }

    public void updateKeySelectZone()
    {
        if (!isPaintZone || GameCanvas.currentDialog != null)
        {
            return;
        }

        bool flag = false;
        if (GameCanvas.keyPressedz[Key.NUM4] || GameCanvas.keyPressedz[23])
        {
            indexSelect--;
            if (indexSelect < 0)
            {
                indexSelect = zones.Length - 1;
            }

            flag = true;
        }
        else if (GameCanvas.keyPressedz[Key.NUM6] || GameCanvas.keyPressedz[22])
        {
            indexSelect++;
            if (indexSelect >= zones.Length)
            {
                indexSelect = 0;
            }

            flag = true;
        }
        else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
        {
            if (indexSelect + zoneCol <= zones.Length - 1)
            {
                indexSelect += zoneCol;
            }

            flag = true;
        }
        else if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
        {
            if (indexSelect - zoneCol >= 0)
            {
                indexSelect -= zoneCol;
            }

            flag = true;
        }

        if (flag)
        {
            scrMain.moveTo(indexSelect / columns * scrMain.ITEM_SIZE);
            GameCanvas.clearKeyHold();
            GameCanvas.clearKeyPressed();
        }

        if (!GameCanvas.isTouch)
        {
            return;
        }

        ScrollResult scrollResult = scrMain.updateKey();
        if (scrollResult.isDowning || scrollResult.isFinish)
        {
            indexSelect = scrollResult.selected;
            if (scrollResult.isFinish)
            {
                Sound.play(Sound.MBClick, 0.5f);
            }
        }
    }

    public void updateKeyAlert()
    {
        if (!isPaintAlert || GameCanvas.currentDialog != null)
        {
            return;
        }

        bool flag = false;
        if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
        {
            indexRow++;
            if (indexRow >= texts.size())
            {
                indexRow = 0;
            }

            flag = true;
        }
        else if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
        {
            indexRow--;
            if (indexRow < 0)
            {
                indexRow = texts.size() - 1;
            }

            flag = true;
        }

        if (flag)
        {
            scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
            GameCanvas.clearKeyHold();
            GameCanvas.clearKeyPressed();
        }

        if (GameCanvas.isTouch)
        {
            ScrollResult scrollResult = scrMain.updateKey();
            if (scrollResult.isDowning || scrollResult.isFinish)
            {
                indexRow = scrollResult.selected;
                flag = true;
            }
        }

        if (!flag || indexRow < 0 || indexRow >= texts.size())
        {
            return;
        }

        // lines of an alert used to offer "open link" or "select this player" buttons; neither exists offline
        center = null;
        ChatTextField.gI().center = null;
    }

    public void updateKeyBuyItemUI()
    {
        if (GameCanvas.currentDialog != null || !isPaintUI())
        {
            return;
        }

        if (NavigateCraftItemGrid())
            return;
        if (indexTitle == 0)
        {
            updateKeyBuyItemUITitle();
        }
        else if (indexTitle >= 1)
        {
            // Offline change (user request): in grid shops the item-info panel no longer captures the
            // arrow keys (it only scrolled the info text); the arrows keep moving the item selection
            // below. The info panel can still be scrolled with the mouse. Same UI list as the grid
            // pointer handling in updateKeyBuyItemUITouch.
            // Upgrade tooltips are read-only detail panes; arrows/D-pad must navigate the slots.
            if (isPaintItemInfo && !IsGridShop && !isPaintUpGrade)
            {
                if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
                {
                    indexRow--;
                    if (indexRow < 0)
                    {
                        indexRow = indexRowMax - 1;
                    }

                    scrInfo.moveTo(indexRow * scrInfo.ITEM_SIZE);
                }
                else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
                {
                    indexRow++;
                    if (indexRow >= indexRowMax)
                    {
                        indexRow = 0;
                    }

                    scrInfo.moveTo(indexRow * scrInfo.ITEM_SIZE);
                }
            }
            else
            {
                if (isPaintAuto)
                {
                    if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
                    {
                        indexRow--;
                        if (indexRow < 0)
                        {
                            indexRow = AutoRowCount - 1; // offline fix: own row count (shared indexRowMax was unreliable)
                        }

                        AutoScrollToRow(indexRow);
                    }
                    else if (GameCanvas.keyPressedz[Key.NUM4] || GameCanvas.keyPressedz[23])
                    {
                        if (indexRow == 0)
                        {
                            Char.aHpValue -= 10;
                            if (Char.aHpValue < 10)
                            {
                                Char.aHpValue = 10;
                            }
                        }
                        else if (indexRow == 1)
                        {
                            Char.aMpValue -= 10;
                            if (Char.aMpValue < 10)
                            {
                                Char.aMpValue = 10;
                            }
                        }
                        else if (indexRow == 2)
                        {
                            Char.aFoodValue -= 10;
                            if (Char.aFoodValue <= 0)
                            {
                                Char.aFoodValue = 1;
                            }
                        }
                    }
                    else if (GameCanvas.keyPressedz[Key.NUM6] || GameCanvas.keyPressedz[22])
                    {
                        if (indexRow == 0)
                        {
                            Char.aHpValue += 10;
                            if (Char.aHpValue > 90)
                            {
                                Char.aHpValue = 90;
                            }
                        }
                        else if (indexRow == 1)
                        {
                            Char.aMpValue += 10;
                            if (Char.aMpValue > 90)
                            {
                                Char.aMpValue = 90;
                            }
                        }
                        else if (indexRow == 2)
                        {
                            if (Char.aFoodValue == 1)
                            {
                                Char.aFoodValue = 10;
                            }
                            else
                            {
                                Char.aFoodValue += 10;
                                if (Char.aFoodValue > 70)
                                {
                                    Char.aFoodValue = 70;
                                }
                            }
                        }
                    }
                    else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
                    {
                        indexRow++;
                        if (indexRow >= AutoRowCount) // offline fix: own row count (shared indexRowMax was unreliable)
                        {
                            indexRow = 0;
                        }

                        AutoScrollToRow(indexRow);
                    }
                }

                // Offline fix: on touch/mouse clients (PC) the keys were not cleared here, so the grid-shop
                // key code below also ran for the auto panel and moved the scroll by indexSelect,
                // overriding the row scroll (list stuck before "Không nhặt gì cả"). Consume them for auto too.
                if (!GameCanvas.isTouch || isPaintAuto)
                {
                    GameCanvas.clearKeyHold();
                    GameCanvas.clearKeyPressed();
                }
                else if (isPaintTrade && indexMenu == 0)
                {
                    int num = 12;
                    if (indexTitle == 1)
                    {
                        if (GameCanvas.keyPressedz[Key.NUM4] || GameCanvas.keyPressedz[23])
                        {
                            indexSelect--;
                            if (indexSelect < 0)
                            {
                                indexSelect = num - 1;
                            }

                            left = (center = null);
                            updateCommandForUI();
                        }
                        else if (GameCanvas.keyPressedz[Key.NUM6] || GameCanvas.keyPressedz[22])
                        {
                            if (indexSelect == 2)
                            {
                                indexTitle = 2;
                                indexSelect = 0;
                            }
                            else if (indexSelect == 5)
                            {
                                indexTitle = 2;
                                indexSelect = 3;
                            }
                            else if (indexSelect == 8)
                            {
                                indexTitle = 2;
                                indexSelect = 6;
                            }
                            else if (indexSelect == 11)
                            {
                                indexTitle = 2;
                                indexSelect = 9;
                            }
                            else if (indexSelect == 14)
                            {
                                indexTitle = 2;
                                indexSelect = 12;
                            }
                            else
                            {
                                indexSelect++;
                                if (indexSelect >= num)
                                {
                                    indexSelect = 0;
                                }
                            }

                            left = (center = null);
                            updateCommandForUI();
                        }
                        else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
                        {
                            if (indexSelect + 3 <= num - 1)
                            {
                                indexSelect += 3;
                            }

                            left = (center = null);
                            updateCommandForUI();
                        }
                        else if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
                        {
                            if (indexSelect >= 0 && indexSelect < 3)
                            {
                                indexTitle = 0;
                                indexSelect = 0;
                            }
                            else if (indexSelect - 3 >= 0)
                            {
                                indexSelect -= 3;
                            }

                            left = (center = null);
                            updateCommandForUI();
                        }
                    }
                    else if (indexTitle == 2)
                    {
                        if (GameCanvas.keyPressedz[Key.NUM4] || GameCanvas.keyPressedz[23])
                        {
                            if (indexSelect == 0)
                            {
                                indexTitle = 1;
                                indexSelect = 2;
                            }
                            else if (indexSelect == 3)
                            {
                                indexTitle = 1;
                                indexSelect = 5;
                            }
                            else if (indexSelect == 6)
                            {
                                indexTitle = 1;
                                indexSelect = 8;
                            }
                            else if (indexSelect == 9)
                            {
                                indexTitle = 1;
                                indexSelect = 11;
                            }
                            else if (indexSelect == 12)
                            {
                                indexTitle = 1;
                                indexSelect = 14;
                            }
                            else
                            {
                                indexSelect--;
                                if (indexSelect < 0)
                                {
                                    indexSelect = num - 1;
                                }
                            }

                            left = (center = null);
                            updateCommandForUI();
                        }
                        else if (GameCanvas.keyPressedz[Key.NUM6] || GameCanvas.keyPressedz[22])
                        {
                            indexSelect++;
                            if (indexSelect >= num)
                            {
                                indexSelect = 0;
                            }

                            left = (center = null);
                            updateCommandForUI();
                        }
                        else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
                        {
                            if (indexSelect + 3 <= num - 1)
                            {
                                indexSelect += 3;
                            }

                            left = (center = null);
                            updateCommandForUI();
                        }
                        else if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
                        {
                            if (indexSelect >= 0 && indexSelect < 3)
                            {
                                indexTitle = 0;
                                indexSelect = 0;
                            }
                            else if (indexSelect - 3 >= 0)
                            {
                                indexSelect -= 3;
                            }

                            left = (center = null);
                            updateCommandForUI();
                        }
                    }

                    if (!GameCanvas.isTouch)
                    {
                        GameCanvas.clearKeyHold();
                        GameCanvas.clearKeyPressed();
                    }
                }
                else if (isPaintLuckySpin)
                {
                    if (GameCanvas.keyPressedz[Key.NUM4] || GameCanvas.keyPressedz[23])
                    {
                        indexSelect--;
                        if (indexSelect < 0)
                        {
                            indexSelect = 8;
                        }
                    }
                    else if (GameCanvas.keyPressedz[Key.NUM6] || GameCanvas.keyPressedz[22])
                    {
                        indexSelect++;
                        if (indexSelect > 8)
                        {
                            indexSelect = 0;
                        }
                    }
                    else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
                    {
                        if (indexSelect + 3 < 9)
                        {
                            indexSelect += 3;
                        }
                    }
                    else if ((GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24]) && indexSelect - 3 >= 0)
                    {
                        indexSelect -= 3;
                    }

                    if (!GameCanvas.isTouch)
                    {
                        GameCanvas.clearKeyHold();
                        GameCanvas.clearKeyPressed();
                    }
                }
                else if (isPaintAuctionSale && indexMenu == 0)
                {
                    if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
                    {
                        indexTitle--;
                        if (indexTitle < 0)
                        {
                            indexTitle = 0;
                        }
                    }
                    else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
                    {
                        indexTitle++;
                        if (indexTitle > 2)
                        {
                            indexTitle = 2;
                        }
                    }

                    updateCommandForUI();
                }
                else if (isPaintAuctionBuy)
                {
                    if (GameCanvas.keyPressedz[Key.NUM4] || GameCanvas.keyPressedz[23])
                    {
                        if (arrItemStands != null)
                        {
                            indexSelect -= 5;
                            if (indexSelect < 0)
                            {
                                indexSelect = arrItemStands.Length - 1;
                            }

                            scrMain.moveTo(indexSelect * scrMain.ITEM_SIZE);
                            updateCommandForUI();
                        }
                    }
                    else if (GameCanvas.keyPressedz[Key.NUM6] || GameCanvas.keyPressedz[22])
                    {
                        if (arrItemStands != null)
                        {
                            indexSelect += 5;
                            if (indexSelect >= arrItemStands.Length)
                            {
                                indexSelect = 0;
                            }

                            scrMain.moveTo(indexSelect * scrMain.ITEM_SIZE);
                            updateCommandForUI();
                        }
                    }
                    else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
                    {
                        if (arrItemStands != null)
                        {
                            indexSelect++;
                            if (indexSelect >= arrItemStands.Length)
                            {
                                indexSelect = 0;
                            }

                            scrMain.moveTo(indexSelect * scrMain.ITEM_SIZE);
                            updateCommandForUI();
                        }
                    }
                    else if ((GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24]) && arrItemStands != null)
                    {
                        indexSelect--;
                        if (indexSelect < 0)
                        {
                            indexTitle = 0;
                        }

                        scrMain.moveTo(indexSelect * scrMain.ITEM_SIZE);
                        updateCommandForUI();
                    }

                    GameCanvas.clearKeyHold();
                    GameCanvas.clearKeyPressed();
                }
                else if ((isPaintGiaoDo || isPaintKhamNgoc || isPaintUpGrade || isPaintConvert) && indexTitle == 1 && indexMenu == 0)
                {
                    if (GameCanvas.keyPressedz[Key.NUM4] || GameCanvas.keyPressedz[23])
                    {
                        indexSelect--;
                        if (indexSelect < 0)
                        {
                            indexSelect = 1;
                        }

                        left = (center = null);
                        updateCommandForUI();
                    }
                    else if (GameCanvas.keyPressedz[Key.NUM6] || GameCanvas.keyPressedz[22])
                    {
                        indexSelect++;
                        if (indexSelect > 1)
                        {
                            indexSelect = 0;
                        }

                        left = (center = null);
                        updateCommandForUI();
                    }
                    else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
                    {
                        indexTitle = 2;
                        indexSelect = 0;
                        left = (center = null);
                        updateCommandForUI();
                    }
                    else if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
                    {
                        indexTitle = 0;
                        indexSelect = 0;
                        left = (center = null);
                        updateCommandForUI();
                    }

                    if (!GameCanvas.isTouch)
                    {
                        GameCanvas.clearKeyHold();
                        GameCanvas.clearKeyPressed();
                    }
                }
                else if ((isPaintSplit || isPaintTinhluyen || isPaintDichChuyen || isPaintLuyenNgoc) && indexTitle == 1 && indexMenu == 0)
                {
                    if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
                    {
                        indexTitle = 2;
                        indexSelect = 0;
                        left = (center = null);
                        updateCommandForUI();
                    }
                    else if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
                    {
                        indexTitle = 0;
                        indexSelect = 0;
                        left = (center = null);
                        updateCommandForUI();
                    }

                    GameCanvas.clearKeyHold();
                    GameCanvas.clearKeyPressed();
                }
                else if ((isPaintGotNgoc || isPaintThaoNgoc) && indexMenu == 0)
                {
                    if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
                    {
                        indexTitle--;
                        if (indexTitle < 0)
                        {
                            indexTitle = 0;
                        }
                    }
                    else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
                    {
                        indexTitle++;
                        if (indexTitle > 1)
                        {
                            indexTitle = 1;
                        }
                    }

                    updateCommandForUI();
                }

                int maxIndexForCurrentStore = getMaxIndexForCurrentStore();
                if (GameCanvas.keyPressedz[Key.NUM4] || GameCanvas.keyPressedz[23])
                {
                    indexSelect--;
                    if (indexSelect < 0)
                    {
                        indexSelect = maxIndexForCurrentStore - 1;
                    }

                    left = (center = null);
                    updateCommandForUI();
                    scrMain.moveTo(indexSelect / columns * scrMain.ITEM_SIZE);
                }
                else if (GameCanvas.keyPressedz[Key.NUM6] || GameCanvas.keyPressedz[22])
                {
                    indexSelect++;
                    if (indexSelect >= maxIndexForCurrentStore)
                    {
                        indexSelect = 0;
                    }

                    left = (center = null);
                    updateCommandForUI();
                    scrMain.moveTo(indexSelect / columns * scrMain.ITEM_SIZE);
                }
                else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
                {
                    if (indexSelect + columns <= maxIndexForCurrentStore - 1)
                    {
                        indexSelect += columns;
                    }

                    left = (center = null);
                    updateCommandForUI();
                    scrMain.moveTo(indexSelect / columns * scrMain.ITEM_SIZE);
                }
                else if (GameCanvas.keyPressedz[Key.NUM2] || GameCanvas.keyPressedz[24])
                {
                    if (indexSelect >= 0 && indexSelect < columns)
                    {
                        indexTitle = 0;
                        indexSelect = 0;
                        if ((isPaintKhamNgoc || isPaintUpGrade || isPaintSplit || isPaintTinhluyen || isPaintDichChuyen || isPaintConvert || isPaintLuyenNgoc) && indexMenu == 0)
                        {
                            indexTitle = 1;
                        }
                    }
                    else if (indexSelect - columns >= 0)
                    {
                        indexSelect -= columns;
                    }

                    left = (center = null);
                    updateCommandForUI();
                    scrMain.moveTo(indexSelect / columns * scrMain.ITEM_SIZE);
                }
            }
        }

        if (GameCanvas.isTouch)
        {
            updateKeyBuyItemUITouch();
        }

        GameCanvas.clearKeyHold();
        GameCanvas.clearKeyPressed();
    }

    private void updateKeyBuyItemUITouch()
    {
        if (GameCanvas.menu.showMenu || GameCanvas.currentDialog != null)
        {
            return;
        }

        if (GameCanvas.isPointerJustRelease && GameCanvas.isPointerHoldIn(popupX, popupY, popupW, Hitem) && (!isPaintItemInfo || GameCanvas.w >= 320) && GameCanvas.isPointerClick)
        {
            if (GameCanvas.isPointerHoldIn(gW2 - 80, popupY + 5, 60, 40))
            {
                indexSelect = 0;
                indexMenu--;
                updateCommandForUI();
                Sound.play(Sound.MBClick, 0.5f);
            }

            if (GameCanvas.isPointerHoldIn(gW2 + 10, popupY + 5, 60, 40))
            {
                indexSelect = 0;
                indexMenu++;
                updateCommandForUI();
                Sound.play(Sound.MBClick, 0.5f);
            }

            indexTitle = 0;
            if (!isPaintUI())
            {
                if (indexMenu < 0)
                {
                    indexMenu = mResources.MENUME.Length - 1;
                }

                if (indexMenu > mResources.MENUME.Length - 1)
                {
                    indexMenu = 0;
                }
            }
            else
            {
                if (isPaintStore)
                {
                    if (indexMenu < 0)
                    {
                        indexMenu = mResources.TITLESTORE.Length - 1;
                    }
                    else if (indexMenu > mResources.TITLESTORE.Length - 1)
                    {
                        indexMenu = 0;
                    }

                    getCurrentStoreData();
                }
                else if (isPaintAuctionBuy)
                {
                    if (indexMenu < 0)
                    {
                        indexMenu = mResources.MENUSTANDS.Length - 1;
                    }
                    else if (indexMenu > mResources.MENUSTANDS.Length - 1)
                    {
                        indexMenu = 0;
                    }

                    Service.gI().menu(0, 28, 0, indexMenu);
                    indexSelect = -1;
                }
                else if (isPaintEliteShop || isPaintAuto)
                {
                    indexMenu = 0;
                }
                else if (indexMenu < 0)
                {
                    indexMenu = 1;
                }
                else if (indexMenu > 1)
                {
                    indexMenu = 0;
                }

                if (isPaintEliteShop)
                {
                    indexMenu = 0;
                }
            }

            isPaintItemInfo = false;
            scrMain.clear();
        }

        if (isPaintItemInfo)
        {
            ScrollResult scrollResult = scrInfo.updateKey();
            if (scrollResult.isDowning || scrollResult.isFinish)
            {
                indexRow = scrollResult.selected;
                indexTitle = 1;
            }

            if (!GameCanvas.isTouchControlLargeScreen)
            {
                return;
            }
        }

        if (isPaintAuto)
        {
            ScrollResult scrollResult2 = scrMain.updateKey();
            if ((scrollResult2.isFinish || scrollResult2.isDowning) && indexSelect != scrollResult2.selected)
            {
                indexTitle = 1;
                indexRow = scrollResult2.selected;
                updateCommandForUI();
            }

            if (GameCanvas.isPointerInRoll(scrMain.xPos + 16, scrMain.yPos + 5, 16, 16, scrMain) && GameCanvas.isPointerClick)
            {
                Char.isAHP = !Char.isAHP;
                if (typeActive == 1)
                {
                    Char.isAHP = false;
                    InfoMe.addInfo(mResources.AOPENACC, 20, mFont.tahoma_7_yellow);
                }

                GameCanvas.isPointerDown = false;
            }

            if (GameCanvas.isPointerInRoll(scrMain.xPos + 16, scrMain.yPos + 35, 16, 16, scrMain) && GameCanvas.isPointerClick)
            {
                Char.isAMP = !Char.isAMP;
                if (typeActive == 1)
                {
                    Char.isAMP = false;
                    InfoMe.addInfo(mResources.AOPENACC, 20, mFont.tahoma_7_yellow);
                }

                GameCanvas.isPointerDown = false;
            }

            if (GameCanvas.isPointerInRoll(scrMain.xPos + 16, scrMain.yPos + 65, 16, 16, scrMain) && GameCanvas.isPointerClick)
            {
                Char.isAFood = !Char.isAFood;
                if (typeActive == 1)
                {
                    Char.isAFood = false;
                    InfoMe.addInfo(mResources.AOPENACC, 20, mFont.tahoma_7_yellow);
                }

                GameCanvas.isPointerDown = false;
            }

            if (GameCanvas.isPointerInRoll(scrMain.xPos + 16, scrMain.yPos + 95, 16, 16, scrMain) && GameCanvas.isPointerClick)
            {
                Char.isABuff = !Char.isABuff;
                GameCanvas.isPointerDown = false;
            }

            if (GameCanvas.isPointerInRoll(scrMain.xPos + 16, scrMain.yPos + 125, 16, 16, scrMain) && GameCanvas.isPointerClick)
            {
                Char.isAPickYen = !Char.isAPickYen;
                GameCanvas.isPointerDown = false;
                if (Char.isAPickYen)
                {
                    Char.isAPickYHM = (Char.isAPickYHMS = (Char.isANoPick = false));
                }
            }

            if (GameCanvas.isPointerInRoll(scrMain.xPos + 16, scrMain.yPos + 155, 16, 16, scrMain) && GameCanvas.isPointerClick)
            {
                Char.isAPickYHM = !Char.isAPickYHM;
                GameCanvas.isPointerDown = false;
                if (Char.isAPickYHM)
                {
                    Char.isAPickYen = (Char.isAPickYHMS = (Char.isANoPick = false));
                }
            }

            if (GameCanvas.isPointerInRoll(scrMain.xPos + 16, scrMain.yPos + 185, 16, 16, scrMain) && GameCanvas.isPointerClick)
            {
                Char.isAPickYHMS = !Char.isAPickYHMS;
                GameCanvas.isPointerDown = false;
                if (Char.isAPickYHMS)
                {
                    Char.isAPickYen = (Char.isAPickYHM = (Char.isANoPick = false));
                }
            }

            if (GameCanvas.isPointerInRoll(scrMain.xPos + 16, scrMain.yPos + 215, 16, 16, scrMain) && GameCanvas.isPointerClick)
            {
                Char.isANoPick = !Char.isANoPick;
                GameCanvas.isPointerDown = false;
                if (Char.isANoPick)
                {
                    Char.isAPickYen = (Char.isAPickYHM = (Char.isAPickYHMS = false));
                }
            }

            // Offline change (user request): click areas of the removed rows are gone too.
            if (GameCanvas.isPointerInRoll(scrMain.xPos + 131, scrMain.yPos + 2, 30, 20, scrMain) && GameCanvas.isPointerClick && Char.isAHP)
            {
                GameCanvas.inputDlg.show(mResources.AHP_TEXT, new Command(mResources.OK, 1511), TField.INPUT_TYPE_NUMERIC);
                GameCanvas.isPointerDown = false;
            }

            if (GameCanvas.isPointerInRoll(scrMain.xPos + 131, scrMain.yPos + 32, 30, 20, scrMain) && GameCanvas.isPointerClick && Char.isAMP)
            {
                GameCanvas.inputDlg.show(mResources.AHP_TEXT, new Command(mResources.OK, 1512), TField.INPUT_TYPE_NUMERIC);
                GameCanvas.isPointerDown = false;
            }

            if (GameCanvas.isPointerInRoll(scrMain.xPos + 131, scrMain.yPos + 62, 30, 20, scrMain) && GameCanvas.isPointerClick && Char.isAFood)
            {
                MyVector myVector = new MyVector();
                myVector.addElement(new Command(mResources.ALEVEL[0], 15130));
                myVector.addElement(new Command(mResources.ALEVEL[1], 15131));
                myVector.addElement(new Command(mResources.ALEVEL[2], 15132));
                myVector.addElement(new Command(mResources.ALEVEL[3], 15133));
                myVector.addElement(new Command(mResources.ALEVEL[4], 15134));
                myVector.addElement(new Command(mResources.ALEVEL[5], 15135));
                myVector.addElement(new Command(mResources.ALEVEL[6], 15136));
                myVector.addElement(new Command(mResources.ALEVEL[7], 15137));
                GameCanvas.menu.startAt(myVector, 0);
                GameCanvas.menu.disableClose = true;
            }
        }

        if (isPaintEliteShop || isPaintStore || isPaintBox || isPaintGrocery || isPaintGroceryLock || isPaintStack || isPaintStackLock || isPaintNonNam || isPaintNonNu || isPaintAoNam || isPaintAoNu || isPaintGangTayNam || isPaintGangTayNu || isPaintQuanNam || isPaintQuanNu || isPaintGiayNam || isPaintGiayNu || isPaintWeapon || isPaintLien || isPaintNhan || isPaintNgocBoi || isPaintPhu || isPaintItemDell)
        {
            ScrollResult scrollResult3 = scrMain.updateKey();
            if ((scrollResult3.isFinish || scrollResult3.isDowning) && indexSelect != scrollResult3.selected)
            {
                indexTitle = 1;
                indexSelect = scrollResult3.selected;
                left = (center = null);
                isPaintItemInfo = false;
                if (scrollResult3.isDowning)
                {
                    Sound.play(Sound.MBClick, 0.5f);
                }

                updateCommandForUI();
            }
        }
        else if (isPaintTrade)
        {
            if (indexMenu == 0)
            {
                indexPointer(popupX + 4, popupY + Hitem + 15, 3, 4, 1);
                indexPointer(popupX + popupW - 3 - 3 * indexSize, popupY + Hitem + 15, 3, 4, 2);
            }
            else
            {
                if (indexMenu != 1)
                {
                    return;
                }

                ScrollResult scrollResult4 = scrMain.updateKey();
                if ((scrollResult4.isFinish || scrollResult4.isDowning) && indexSelect != scrollResult4.selected)
                {
                    indexTitle = 1;
                    indexSelect = scrollResult4.selected;
                    if (scrollResult4.isDowning)
                    {
                        Sound.play(Sound.MBClick, 0.5f);
                    }

                    updateCommandForUI();
                }
            }
        }
        else if (isPaintUpPearl || isPaintLuyenThach)
        {
            if (indexMenu == 0)
            {
                indexPointer(popupX + 4, popupY + Hitem + 3, 6, 4, 1);
            }
            else
            {
                if (indexMenu != 1)
                {
                    return;
                }

                ScrollResult scrollResult5 = scrMain.updateKey();
                if ((scrollResult5.isFinish || scrollResult5.isDowning) && indexSelect != scrollResult5.selected)
                {
                    indexTitle = 1;
                    indexSelect = scrollResult5.selected;
                    if (scrollResult5.isDowning)
                    {
                        Sound.play(Sound.MBClick, 0.5f);
                    }

                    updateCommandForUI();
                }
            }
        }
        else if (isPaintSplit || isPaintTinhluyen || isPaintDichChuyen || isPaintLuyenNgoc)
        {
            if (indexMenu == 0)
            {
                if (GameCanvas.isPointerHoldIn(popupX + 74, ystart - indexSize - 3, indexSize, indexSize))
                {
                    indexTitle = 1;
                    indexSelect = 0;
                    Sound.play(Sound.MBClick, 0.5f);
                    updateCommandForUI();
                }

                indexPointer(popupX + 4, popupY + 2 * Hitem + 5, 6, 4, 2);
            }
            else
            {
                if (indexMenu != 1)
                {
                    return;
                }

                ScrollResult scrollResult6 = scrMain.updateKey();
                if ((scrollResult6.isFinish || scrollResult6.isDowning) && indexSelect != scrollResult6.selected)
                {
                    indexTitle = 1;
                    indexSelect = scrollResult6.selected;
                    if (scrollResult6.isDowning)
                    {
                        Sound.play(Sound.MBClick, 0.5f);
                    }

                    updateCommandForUI();
                }
            }
        }
        else if (isPaintUpGrade || isPaintConvert || isPaintKhamNgoc)
        {
            if (indexMenu == 0)
            {
                doTouchUpgrade();
            }
            else
            {
                if (indexMenu != 1)
                {
                    return;
                }

                ScrollResult scrollResult7 = scrMain.updateKey();
                if ((scrollResult7.isFinish || scrollResult7.isDowning) && indexSelect != scrollResult7.selected)
                {
                    indexTitle = 1;
                    indexSelect = scrollResult7.selected;
                    if (scrollResult7.isDowning)
                    {
                        Sound.play(Sound.MBClick, 0.5f);
                    }

                    updateCommandForUI();
                }
            }
        }
        else if (isPaintGiaoDo)
        {
            if (indexMenu == 0)
            {
                ScrollResult scrollResult8 = scrMain.updateKey();
                if ((scrollResult8.isFinish || scrollResult8.isDowning) && indexSelect != scrollResult8.selected)
                {
                    indexTitle = 1;
                    indexSelect = scrollResult8.selected;
                    left = (center = null);
                    isPaintItemInfo = false;
                    updateCommandForUI();
                }
            }
            else if (indexMenu == 1)
            {
                ScrollResult scrollResult9 = scrMain.updateKey();
                if ((scrollResult9.isFinish || scrollResult9.isDowning) && indexSelect != scrollResult9.selected)
                {
                    indexTitle = 1;
                    indexSelect = scrollResult9.selected;
                    updateCommandForUI();
                }
            }
        }
        else if (isPaintAuctionSale)
        {
            if (indexMenu == 0)
            {
                if (GameCanvas.isPointerHoldIn(popupX + 75, popupY + 69, indexSize, indexSize))
                {
                    if (GameCanvas.isPointerClick && GameCanvas.isPointerJustRelease)
                    {
                        indexTitle = 1;
                        updateCommandForUI();
                    }
                }
                else if (GameCanvas.isPointerHoldIn(tfText.x, tfText.y, tfText.width, tfText.height) && GameCanvas.isPointerClick && GameCanvas.isPointerJustRelease)
                {
                    indexTitle = 2;
                    tfText.isFocus = true;
                    right = tfText.cmdClear;
                    updateCommandForUI();
                }
            }
            else if (indexMenu == 1)
            {
                ScrollResult scrollResult10 = scrMain.updateKey();
                if ((scrollResult10.isFinish || scrollResult10.isDowning) && indexSelect != scrollResult10.selected)
                {
                    indexTitle = 1;
                    indexSelect = scrollResult10.selected;
                    updateCommandForUI();
                }
            }
        }
        else if (isPaintLuckySpin)
        {
            int num = ((!Main.isPC) ? 36 : 40);
            if (GameCanvas.isPointerHoldIn(xstart, ystart, num * 3, num * 3) && GameCanvas.isPointerJustRelease && GameCanvas.isPointerClick)
            {
                int num2 = (GameCanvas.pxLast - xstart) / num + (GameCanvas.pyLast - ystart) / num * 3;
                indexSelect = num2;
                indexTitle = 1;
                updateCommandForUI();
                doviewCardsInfo();
            }
        }
        else
        {
            if (!isPaintGotNgoc && !isPaintThaoNgoc)
            {
                return;
            }

            if (indexMenu == 0)
            {
                if (GameCanvas.isPointerHoldIn(popupX + 75, popupY + 69, indexSize, indexSize) && GameCanvas.isPointerClick && GameCanvas.isPointerJustRelease)
                {
                    indexTitle = 1;
                    updateCommandForUI();
                }
            }
            else if (indexMenu == 1)
            {
                ScrollResult scrollResult11 = scrMain.updateKey();
                if ((scrollResult11.isFinish || scrollResult11.isDowning) && indexSelect != scrollResult11.selected)
                {
                    indexTitle = 1;
                    indexSelect = scrollResult11.selected;
                    updateCommandForUI();
                }
            }
        }
    }

    private void updateKeyBuyItemUITitle()
    {
        left = (center = null);
        if (isPaintLuckySpin)
        {
            return;
        }

        if (isPaintItemDell)
        {
            left = null;
        }
        else if (isPaintBox)
        {
            if (svTitle.Equals(string.Empty))
            {
                if (indexMenu == 0)
                {
                    left = new Command(mResources.MENU, 11115);
                }
                else if (indexMenu == 1)
                {
                    left = new Command(mResources.MENU, 11116);
                }
            }
            else
            {
                left = null;
            }
        }
        else if (indexMenu == 1 && isPaintTrade && typeTrade == 0)
        {
            left = cmdTradeSendMoney;
        }
        else if (indexMenu == 1 && !isPaintStore && !isPaintEliteShop && !isPaintUpPearl && !isPaintLuyenThach && !isPaintUpGrade && !isPaintConvert && !isPaintSplit && !isPaintTinhluyen && !isPaintDichChuyen && !isPaintTrade && !isPaintAuctionBuy && !isPaintAuctionSale && !isPaintLuyenNgoc && !isPaintKhamNgoc && !isPaintGiaoDo && !isPaintGotNgoc)
        {
            left = new Command(mResources.SORT, 110221);
        }

        if (isPaintLuyenThach && indexMenu == 0)
        {
            if (isCanLuyenThach())
            {
                left = new Command(mResources.BEGIN, 1600);
            }
        }
        else if (isPaintTinhluyen)
        {
            try
            {
                if (indexMenu == 0)
                {
                    if (indexTitle == 1)
                    {
                        if (itemSplit != null)
                        {
                            left = new Command(mResources.SELECT, 11103);
                        }
                        else
                        {
                            isPaintItemInfo = false;
                        }

                        if (GameCanvas.isTouchControlLargeScreen)
                        {
                            updateItemInfo(3, itemSplit);
                        }
                        else
                        {
                            center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11104, itemSplit);
                        }
                    }
                    else if (indexTitle == 2)
                    {
                        Item item = arrItemSplit[indexSelect];
                        if (item != null)
                        {
                            if (GameCanvas.isTouchControlLargeScreen)
                            {
                                updateItemInfo(3, item);
                            }
                            else
                            {
                                center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11104, item);
                            }

                            left = new Command(mResources.MOVEOUT, 1605);
                        }
                        else
                        {
                            isPaintItemInfo = false;
                        }

                        if (itemSplit != null)
                        {
                            left = new Command(mResources.SELECT, 1604);
                        }
                    }
                }
                else if (indexMenu == 1)
                {
                    if (Char.getMyChar().arrItemBag[indexSelect] == null)
                    {
                        left = null;
                        isPaintItemInfo = false;
                    }
                    else
                    {
                        left = new Command(mResources.SELECT, 11106);
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        else if (isPaintDichChuyen)
        {
            if (indexMenu == 0)
            {
                if (indexTitle == 1)
                {
                    if (itemSplit != null && itemSplit.upgrade > 13)
                    {
                        left = new Command(mResources.SELECT, 11103);
                    }
                    else if (itemSplit != null)
                    {
                        left = cmdSplitMoveOut;
                    }
                    else
                    {
                        isPaintItemInfo = false;
                    }

                    if (GameCanvas.isTouchControlLargeScreen)
                    {
                        updateItemInfo(3, itemSplit);
                    }
                    else
                    {
                        center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11104, itemSplit);
                    }
                }
                else if (indexTitle == 2)
                {
                    Item item2 = arrItemSplit[indexSelect];
                    if (item2 != null)
                    {
                        if (GameCanvas.isTouchControlLargeScreen)
                        {
                            updateItemInfo(3, item2);
                        }
                        else
                        {
                            center = new Command((!GameCanvas.isTouch) ? string.Empty : mResources.VIEW, 11104, item2);
                        }
                    }
                    else
                    {
                        isPaintItemInfo = false;
                    }

                    if (itemSplit != null && itemSplit.upgrade > 13)
                    {
                        left = new Command(mResources.SELECT, 1604);
                    }
                }
            }
            else if (indexMenu == 1)
            {
                if (Char.getMyChar().arrItemBag[indexSelect] == null)
                {
                    left = null;
                    isPaintItemInfo = false;
                }
                else
                {
                    left = new Command(mResources.SELECT, 1606);
                }
            }
        }

        if (isPaintUpPearl && indexMenu == 0)
        {
            int num = 0;
            for (int i = 0; i < arrItemUpPeal.Length; i++)
            {
                if (arrItemUpPeal[i] != null)
                {
                    num++;
                    if (num >= 2)
                    {
                        left = new Command(mResources.BEGIN, 11062);
                        break;
                    }
                }
            }
        }

        if (isPaintUpGrade && indexMenu == 0 && itemUpGrade != null)
        {
            for (int j = 0; j < arrItemUpGrade.Length; j++)
            {
                if (arrItemUpGrade[j] != null)
                {
                    center = new Command(string.Empty, 110981);
                    left = new Command(mResources.BEGIN, 110981);
                    break;
                }
            }
        }

        if (isPaintKhamNgoc && indexMenu == 0 && itemUpGrade != null && itemSplit != null)
        {
            for (int k = 0; k < arrItemUpGrade.Length; k++)
            {
                if (arrItemUpGrade[k] != null)
                {
                    center = new Command(string.Empty, 341);
                    left = new Command(mResources.BEGIN, 341);
                    break;
                }
            }
        }

        if (isPaintAuctionSale && indexMenu == 0 && itemSell != null && tfText != null && !tfText.getText().Equals(string.Empty) && Char.getMyChar().xu >= 5000)
        {
            left = new Command(mResources.SALE, 15002);
        }

        if (isPaintGotNgoc && indexMenu == 0 && itemSplit != null && Char.getMyChar().xu >= coinGotngoc[itemSplit.upgrade])
        {
            left = new Command(mResources.BEGIN, 111031);
        }

        if (isPaintConvert && indexMenu == 0 && indexTitle == 0)
        {
            for (int l = 0; l < arrItemConvert.Length; l++)
            {
                if (arrItemConvert[l] == null)
                {
                    left = null;
                    break;
                }

                if (l == arrItemConvert.Length - 1)
                {
                    left = new Command(mResources.BEGIN, 140131);
                }
            }
        }

        if (isPaintSplit && indexMenu == 0 && itemSplit != null && itemSplit.upgrade > 0)
        {
            left = new Command(mResources.BEGIN, 11105);
        }

        if (isPaintTrade && indexMenu == 0)
        {
            if (typeTrade == 0)
            {
                left = cmdTradeLock;
            }
            else if (typeTrade == 1 && typeTradeOrder >= 1 && timeTrade - mSystem.getCurrentTimeMillis() / 1000 <= 0)
            {
                left = cmdTradeAccept;
            }
        }

        if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
        {
            indexTitle = 1;
            indexSelect = 0;
            indexRow = -1;
            scrMain.clear();
            scrInfo.clear();
            updateCommandForUI();
        }

        if (GameCanvas.keyPressedz[Key.NUM4] || GameCanvas.keyPressedz[23])
        {
            indexSelect = 0;
            indexRow = -1;
            indexMenu--;
            scrMain.clear();
            scrInfo.clear();
            if (isPaintAuctionBuy)
            {
                if (indexMenu < 0)
                {
                    indexMenu = mResources.MENUSTANDS.Length - 1;
                }

                Service.gI().menu(0, 28, 0, indexMenu);
                arrItemStands = null;
                indexSelect = -1;
            }
            else if (isPaintEliteShop || isPaintAuto)
            {
                indexMenu = 0;
            }
            else if (indexMenu < 0)
            {
                if (isPaintStore)
                {
                    indexMenu = mResources.TITLESTORE.Length - 1;
                }
                else
                {
                    indexMenu = 1;
                }
            }

            left = (center = null);
            if (isPaintStore)
            {
                getCurrentStoreData();
            }

            if (isPaintEliteShop && arrItemElites == null && indexMenu == 0)
            {
                Service.gI().requestItem(35);
            }
        }

        if (!GameCanvas.keyPressedz[Key.NUM6] && !GameCanvas.keyPressedz[22])
        {
            return;
        }

        indexSelect = 0;
        indexRow = -1;
        indexMenu++;
        scrMain.clear();
        scrInfo.clear();
        if (isPaintAuctionBuy)
        {
            if (indexMenu > mResources.MENUSTANDS.Length - 1)
            {
                indexMenu = 0;
            }

            Service.gI().menu(0, 28, 0, indexMenu);
            arrItemStands = null;
            indexSelect = -1;
        }
        else if (isPaintEliteShop || isPaintAuto)
        {
            indexMenu = 0;
        }
        else if (isPaintStore)
        {
            if (indexMenu > 2)
            {
                indexMenu = 0;
            }
        }
        else if (indexMenu > 1)
        {
            indexMenu = 0;
        }

        left = (center = null);
        if (isPaintStore)
        {
            if (indexMenu == 0)
            {
                Service.gI().requestItem(14);
            }
            else if (indexMenu == 1)
            {
                Service.gI().requestItem(15);
            }
            else if (indexMenu == 2)
            {
                Service.gI().requestItem(32);
            }
        }

        if (isPaintEliteShop && arrItemElites == null && indexMenu == 0)
        {
            Service.gI().requestItem(35);
        }
    }

    public void indexPointer(int x, int y, int numCols, int numRows, int title)
    {
        if (!isPaintUpPearl && !isPaintLuyenThach && !isPaintTinhluyen && !isPaintDichChuyen && !isPaintSplit && !isPaintTrade && !isPaintUpGrade && !isPaintConvert && !isPaintAuctionSale && !isPaintLuyenNgoc && !isPaintKhamNgoc && !isPaintGotNgoc && !isPaintThaoNgoc)
        {
            return;
        }

        int w = numCols * indexSize;
        int h = numRows * indexSize;
        scrMain.clear();
        if (!GameCanvas.isPointerHoldIn(x, y, w, h))
        {
            return;
        }

        indexTitle = title;
        if (!GameCanvas.isPointerClick)
        {
            return;
        }

        trans = false;
        int num = (GameCanvas.pxLast - x) / indexSize + (GameCanvas.pyLast - y) / indexSize * numCols;
        if (num / columns < rows)
        {
            indexSelect = num;
        }

        left = (center = null);
        if (isPaintTrade)
        {
            if (indexSelect < 0)
            {
                indexSelect = 11;
            }

            if (indexSelect > 11)
            {
                indexSelect = 11;
            }
        }

        if (GameCanvas.isPointerJustRelease)
        {
            Sound.play(Sound.MBClick, 0.5f);
        }

        updateCommandForUI();
    }

    public void updateKeyCaptcha()
    {
        if (GameCanvas.keyHold[Key.NUM4] || GameCanvas.keyHold[23])
        {
            doClickCaptcha(0);
            GameCanvas.clearKeyHold();
        }
        else if (GameCanvas.keyHold[Key.NUM2] || GameCanvas.keyHold[24])
        {
            doClickCaptcha(1);
            GameCanvas.clearKeyHold();
        }
        else if (GameCanvas.keyHold[Key.NUM6] || GameCanvas.keyHold[22])
        {
            doClickCaptcha(2);
            GameCanvas.clearKeyHold();
        }
    }

    public void updateKeyThanThu()
    {
        if (GameCanvas.keyHold[Key.NUM2] || GameCanvas.keyHold[24])
        {
            if (indexRow == 0)
            {
                indexTitle--;
                indexRow = -1;
            }
            else
            {
                indexRow--;
            }

            scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
        }
        else if (GameCanvas.keyPressedz[Key.NUM8] || GameCanvas.keyPressedz[25])
        {
            indexRow++;
            if (indexRow >= indexRowMax)
            {
                indexRow = 0;
            }

            scrMain.moveTo(indexRow * scrMain.ITEM_SIZE);
        }
        else if (GameCanvas.keyPressedz[Key.NUM4] || GameCanvas.keyPressedz[23])
        {
            indexThanThu = (sbyte)(indexThanThu - 1);
            if (indexThanThu < 0)
            {
                indexThanThu = 0;
            }
        }
        else if (GameCanvas.keyPressedz[Key.NUM6] || GameCanvas.keyPressedz[22])
        {
            indexThanThu = (sbyte)(indexThanThu + 1);
            if (indexThanThu > clan_thanthu.size() - 1)
            {
                indexThanThu = (sbyte)(clan_thanthu.size() - 1);
            }
        }
    }
}
