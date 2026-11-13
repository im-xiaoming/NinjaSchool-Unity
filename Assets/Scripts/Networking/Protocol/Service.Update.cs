using Functions.Auto;
using System;
using UnityEngine;

public partial class Service
{
    public void updateData()
    {
        Message message = null;
        try
        {
            message = messageNotMap(-122);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }

    public void updateActive(int passOld, int passNew)
    {
        Message message = null;
        try
        {
            message = messageNotMap(-104);
            message.writer().writeInt(passOld);
            message.writer().writeInt(passNew);
            session.sendMessage(message);
        }
        catch (Exception ex)
        {
            Out.println(ex.Message + ex.StackTrace);
        }
        finally
        {
            message.cleanup();
        }
    }
}
