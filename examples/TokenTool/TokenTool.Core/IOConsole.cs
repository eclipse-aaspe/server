
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp;

/// <summary>
/// Minimaler I/O‑Adapter für die 1:1‑Weiterverwendung von Console.ReadLine/WriteLine
/// in Blazor Server. In der Console‑App zeigen die Delegates auf System.Console,
/// in Blazor werden sie von der UI (Razor) neu verdrahtet.
/// </summary>
public class IOConsole
{
    // ---------- OUTPUT (WriteLine) ----------
    /// <summary>
    /// Ziel für Ausgaben. In der Console-App: System.Console.WriteLine.
    /// In Blazor: UI fügt Zeilen in eine Liste ein und ruft StateHasChanged().
    /// </summary>
    public Action<string?> WriteLineConsumer { get; set; } = s => System.Console.WriteLine(s ?? "");

    /// <summary>
    /// Entspricht Console.WriteLine(text).
    /// </summary>
    public void WriteLine(string? text) => WriteLineConsumer(text);


    // ---------- INPUT (ReadLine) ----------
    private TaskCompletionSource<string?>? _nextLineTcs;
    private readonly object _lock = new();

    /// <summary>
    /// Blockiert so lange, bis die UI eine Zeile via SubmitLine(...) liefert.
    /// Wird synchron aufgerufen, um Console.ReadLine 1:1 zu emulieren.
    /// </summary>
    public string? ReadLine()
    {
        Task<string?> waitTask;

        lock (_lock)
        {
            // Alte TCS (falls vorhanden) sauber verwerfen
            _nextLineTcs?.TrySetCanceled();

            // Neue TCS für die nächste Eingabe
            _nextLineTcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            waitTask = _nextLineTcs.Task;
        }

        try
        {
            // Warten, bis die UI eine Zeile liefert (SubmitLine)
            waitTask.Wait();
            return waitTask.Result;
        }
        catch (AggregateException ae) when (ae.InnerException is TaskCanceledException)
        {
            // Falls jemand Reset() oder CancelRead() ruft
            return string.Empty;
        }
    }

    /// <summary>
    /// Wird von der Blazor‑UI (z. B. beim Enter‑Key oder Button) aufgerufen.
    /// Liefert die aktuelle Eingabezeile an das wartende ReadLine().
    /// </summary>
    public void SubmitLine(string? line)
    {
        TaskCompletionSource<string?>? tcs;
        lock (_lock)
        {
            tcs = _nextLineTcs;
            _nextLineTcs = null;
        }
        tcs?.TrySetResult(line ?? string.Empty);
    }

    /// <summary>
    /// Bricht das aktuelle ReadLine()-Warten ab (optional).
    /// </summary>
    public void CancelRead()
    {
        TaskCompletionSource<string?>? tcs;
        lock (_lock)
        {
            tcs = _nextLineTcs;
            _nextLineTcs = null;
        }
        tcs?.TrySetCanceled();
    }


    // ---------- OPTIONALE HILFSWERTE FÜR DEINEN "f"-PFAD (PFX) ----------
    /// <summary>Von der UI hochgeladene .pfx-Bytes (für Modus 'f').</summary>
    public byte[]? UploadedPfxBytes { get; set; }

    /// <summary>Optionales PFX‑Passwort (wenn nicht im Code fest verdrahtet).</summary>
    public string? PfxPassword { get; set; }


    // ---------- QUALITY-OF-LIFE: RESET ----------
    /// <summary>
    /// Setzt den Zustand zurück (z. B. beim Neustart des Tools).
    /// </summary>
    public void Reset()
    {
        CancelRead();
        UploadedPfxBytes = null;
        PfxPassword = null;
    }
}
