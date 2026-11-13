using Functions.Auto;
using System;
using UnityEngine;

public partial class Service
{
    public void luckyDraw(short type, string money, sbyte typeLucky)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)92);
            message.writer().writeShort(type);
            message.writer().writeUTF(money);
            message.writer().writeByte(typeLucky);
            session.sendMessage(message);
        }
        catch (Exception)
        {
        }
        finally
        {
            message.cleanup();
        }
    }
}
