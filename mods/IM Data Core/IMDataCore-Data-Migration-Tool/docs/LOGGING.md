# Diagnostic logging

IMDataCore Data Migration Tool 1.6.1+ writes a diagnostic log for every process launch.

Default log directory on Windows:

```text
%LOCALAPPDATA%\Cosmo\IMDataCore Data Migration Tool\Logs
```

The main GUI has a **Logs** button that opens this directory in Windows Explorer. The current session log path is also written into the GUI validation/migration log when the main window successfully appears.

Startup is logged before WinForms constructs the main window. If the GUI fails during construction or initialization, Data Migration Tool catches the startup exception where possible and displays a fallback error dialog containing the exact log path.

The application also records:

- process and runtime information;
- GUI startup milestones;
- uncaught WinForms UI-thread exceptions;
- unhandled AppDomain exceptions;
- unobserved task exceptions;
- messages written to the main validation/migration log;
- bulk-scan failures;
- branch-repair analysis failures;
- normal GUI shutdown.

Logging is intentionally best-effort: failure to create or write a log must not itself stop Data Migration Tool from launching.
