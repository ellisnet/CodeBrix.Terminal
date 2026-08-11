namespace CodeBrix.Terminal.Engine; //was previously: namespace XtermSharp;

/// <summary>
/// The callback surface a <see cref="Terminal"/> host implements to react to
/// engine events: cursor visibility, title changes, escape-driven resizes,
/// data the engine needs sent back to the remote end, and window manipulation
/// commands. Extend <see cref="SimpleTerminalDelegate"/> to override only the
/// members you need.
/// </summary>
public interface ITerminalDelegate {
    /// <summary>
    /// Called when the cursor should be made visible again (for example after
    /// an escape sequence re-enables it). A rendering host typically
    /// invalidates/repaints so the cursor shows.
    /// </summary>
    void ShowCursor (Terminal source);

    /// <summary>
    /// This event is raised when the title of the terminal has been changed
    /// </summary>
    void SetTerminalTitle (Terminal source, string title);

    /// <summary>
    /// This event is raised when the icon title of the terminal has been changed
    /// </summary>
    void SetTerminalIconTitle (Terminal source, string title);

    /// <summary>
    /// This event is triggered from the engine, when the request to resize the window is received from an escape sequence.
    /// </summary>
    void SizeChanged (Terminal source);

    /// <summary>
    /// Used to respond to the client running on the other end.  This information should be sent to the remote end or subshell.
    /// </summary>
    void Send (byte [] data);

    /// <summary>
    /// These are various commands that are sent by the client.  They are rare,
    /// and if you do not know what to return, just return null, the terminal
    /// will return a suitable value.
    /// 
    /// The response string needs to be suitable for the Xterm CSI Ps; Ps ; Ps t command
    /// see the WindowManipulationCommand enumeration for those that need to return values
    /// </summary>
    string WindowCommand (Terminal source, WindowManipulationCommand command, params int[] args);

    /// <summary>
    /// This method should return `true` if operations that can read the buffer back should be allowed,
    /// otherwise, return false.   This is useful to run some applications that attempt to checksum the
    /// contents of the screen (unit tests)
    /// </summary>
    bool IsProcessTrusted ();
}

/// <summary>
/// A simple ITerminalDelegate that does nothing
/// </summary>
public class SimpleTerminalDelegate : ITerminalDelegate {
    public virtual void Send (byte [] data)
    {
    }

    public virtual void SetTerminalTitle (Terminal source, string title)
    {
    }

    public virtual void SetTerminalIconTitle (Terminal source, string title)
    {
    }

    public virtual void ShowCursor (Terminal source)
    {
    }

    public virtual void SizeChanged (Terminal source)
    {
    }

    public virtual string WindowCommand (Terminal source, WindowManipulationCommand command, params int [] args)
    {
        return null;
    }

    public virtual bool IsProcessTrusted ()
    {
        return true;
    }
}