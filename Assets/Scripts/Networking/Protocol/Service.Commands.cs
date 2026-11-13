using Functions.Auto;
using System;
using UnityEngine;

public partial class Service
{
    public void doGetImgIcon(short id)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)125);
            message.writer().writeByte(1);
            message.writer().writeShort(id);
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

    public void doGetByteData(int id)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)125);
            message.writer().writeByte(2);
            message.writer().writeShort(id);
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
