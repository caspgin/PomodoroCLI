// See https://aka.ms/new-console-template for more information
using System;
using System.Drawing;
using Spectre.Console;
using Timer = System.Timers.Timer;
using System.Text.Json;
using H.NotifyIcon;
using Microsoft.Toolkit.Uwp.Notifications;

class AppConfig
{
    public string SaveProjectFilePath { get; set; } = "";
    public int DefaultSessionTime { get; set; } = 25 * 60;
    public int DefaultBreakTime { get; set; } = 5 * 60;
    public int DefaultMessageTime { get; set; } = 20;
}
class ProjectSpace
{
    public string ProjectName { get; set; }
    public List<PomodoroItem> Queue { get; set; } = new List<PomodoroItem>();
}

class PomodoroItem
{
    public enum StatusName
    {
        COMPLETED,
        IN_PROGRESS,
        PENDING
    }
    public string SessionName { get; set; } 
    public int NumberOfSessions { get; set; }
    public int TimePerSession { get; set; }
    
    public StatusName Status{ get; set; }
    public int CompletedSessions { get; set; }
    
    public PomodoroItem(string sessionName, int timePerSession, int numberOfSessions = 1)
    {
        SessionName = sessionName;
        TimePerSession = timePerSession;
        NumberOfSessions = numberOfSessions;
        Status = StatusName.PENDING;
        CompletedSessions = 0;
    }
}

class Commands
{
    public required string Name { get; set; }
}

class Program
{
    enum ActivityName
    {
        SESSION,
        BREAK,
        
    }

    enum TimerStatus
    {
        RUNNING,
        PAUSED,
        FINISHED,
    }

    private static AppConfig _appConfig;
    private static int _sessionTime;
    private static int _breakTime;
    private static ActivityName _activityName;
    private static bool _running = true;
    private static System.Timers.Timer _timer;
    private static System.Timers.Timer _messageTimer;
    private static int _currentTime;
    private static List<Commands> _commandHistory = new List<Commands>();
    static string _command = "";
    static List<PomodoroItem> _pomodoroList = new List<PomodoroItem>();
    private static string _message = "";
    private static int _messageTimeLimit;
    private static bool _showCompletedSessions = false;
    private static TimerStatus _timerStatus;
    private static int _lastCommandIndex = _commandHistory.Count;
    private static string _projectName = "";

    static void ClearFromCursorToEnd()
    {
        int left = Console.CursorLeft;
        int top = Console.CursorTop;
        int originalLeft = left;
        int originalTop = top;
        int width = Console.BufferWidth;
        int height = Console.BufferHeight;
        while (left < width - 1 || top < height - 1)
        {
            Console.Write(' ');
            left = Console.CursorLeft;
            top = Console.CursorTop;
        }

        // Restore cursor position
        Console.SetCursorPosition(originalLeft, originalTop);
    }

    static void ClearLineRight()
    {
        int left = Console.CursorLeft;
        int width = Console.BufferWidth;
        Console.Write(new string(' ', width - left));
    }

    static void CursorToHome()
    {
        Console.SetCursorPosition(0, 0);
    }



    static void DrawHeader()
    {
        int width = Console.BufferWidth;
        AnsiConsole.Markup($"[bold]Pomodoro CLI[/] : [red3]{_activityName.ToString()}[/]");
        ClearLineRight();
        if (_projectName != "")
        {
            AnsiConsole.Markup($"[bold]Project Name:[/][blue]{_projectName}[/] ");
            ClearLineRight();
        }
    }

    static void DrawTimer()
    {
        int minutes = _currentTime / 60;
        int seconds = _currentTime - (minutes * 60);
        Console.Write(@"Time Left :  {0:00}:{1:00}", minutes, seconds);
        ClearLineRight();
        Console.Write(new string('-', Console.BufferWidth - 1));
        ClearLineRight();
    }

    static void DrawQueue()
    {
        AnsiConsole.Markup("[bold yellow]Queue:[/]");
        ClearLineRight();
        int i = 1;

        foreach (PomodoroItem item in _pomodoroList)
        {
            if (item.Status == PomodoroItem.StatusName.COMPLETED)
            {
                continue;
            }

            AnsiConsole.MarkupInterpolated(
                $@"[bold]{i}.[/] {item.SessionName}        {item.CompletedSessions}/{item.NumberOfSessions} x {item.TimePerSession / 60} min  {item.Status.ToString().ToLower()}");
            ClearLineRight();
            i++;
        }

        if (i == 1)
        {
            AnsiConsole.Markup("No PENDING sessions in the Queue");
            ClearLineRight();
        }

        AnsiConsole.Markup($"[bold yellow]Completed Sessions:[/] [italic]use command 'completed' to see[/]");
        ClearLineRight();
        if (_showCompletedSessions)
        {
            if (_pomodoroList.Count == 0)
            {
                AnsiConsole.Markup("No Completed Sessions");
                ClearLineRight();
            }
            else
            {
                i = 1;
                foreach (PomodoroItem item in _pomodoroList)
                {
                    if (item.Status != PomodoroItem.StatusName.COMPLETED)
                    {
                        continue;
                    }

                    AnsiConsole.MarkupInterpolated(
                        $@"[bold]{i}.[/] {item.SessionName}        {item.CompletedSessions}/{item.NumberOfSessions} x {item.TimePerSession / 60.0} min");
                    ClearLineRight();
                    i++;
                }
            }
        }

        ClearLineRight();
        ClearLineRight();
    }

    static void DrawMessage()
    {
        //TODO: Add a message Queue instead of message string
        AnsiConsole.MarkupInterpolated($"[bold yellow]MESSAGE:[/] {_message.Trim()}");
        ClearLineRight();
        ClearLineRight();
        ClearLineRight();
    }

    static void DrawCommands()
    {
        AnsiConsole.Markup("[bold yellow]Commands:[/]");
        ClearLineRight();

        Console.Write(">");
    }

    static void RenderUi(string data = "")
    {
        //TODO: Add a splash page
        //TODO: Add ASCII art timer
        //TODO: Better completion art and celebration
        CursorToHome();
        DrawHeader();
        DrawTimer();
        DrawQueue();
        DrawMessage();
        DrawCommands();
    }

    static void MessageTimerInit()
    {
        _messageTimer = new Timer(1000);
        _messageTimer.AutoReset = true;
        _messageTimeLimit = _appConfig.DefaultMessageTime;
        _messageTimer.Elapsed += (sender, args) =>
        {
            _messageTimeLimit--;
            if (_messageTimeLimit <= 0)
            {
                _message = "";
                _messageTimeLimit = _appConfig.DefaultMessageTime;
            }
        };
        _messageTimer.Start();
    }

    static void CompleteCurrentInQueue()
    {

        foreach (var item in _pomodoroList)
        {
            if (item.Status == PomodoroItem.StatusName.IN_PROGRESS)
            {
                item.CompletedSessions++;
                if (item.CompletedSessions == item.NumberOfSessions)
                {
                    item.Status = PomodoroItem.StatusName.COMPLETED;
                }

                break;
            }
        }
    }

    //Sets the session as current 
    static void SetNextInQueue()
    {
        foreach (var pomodoroItem in _pomodoroList)
        {
            if (pomodoroItem.Status == PomodoroItem.StatusName.IN_PROGRESS)
            {
                if (pomodoroItem.CompletedSessions == pomodoroItem.NumberOfSessions)
                {
                    pomodoroItem.Status = PomodoroItem.StatusName.COMPLETED;
                }
                else
                {
                    pomodoroItem.Status = PomodoroItem.StatusName.PENDING;
                }
            }
            if (pomodoroItem.Status == PomodoroItem.StatusName.PENDING)
            {
                _currentTime = pomodoroItem.TimePerSession;
                pomodoroItem.Status = PomodoroItem.StatusName.IN_PROGRESS;
                break;
            }
        }
    }

    static void TimerInit(int timerTime, int breakTime)
    {
        _timer = new Timer(1000);
        _sessionTime = timerTime != 0 ? timerTime : _appConfig.DefaultSessionTime;
        _breakTime = breakTime != 0 ? breakTime : _appConfig.DefaultBreakTime;
        _currentTime = _sessionTime;
        _timerStatus = TimerStatus.FINISHED;
        _timer.AutoReset = true;
        _timer.Elapsed += async (sender, args) =>
        {
            _currentTime--;
            if (_currentTime < 0)
            {
                //stop Timer and Make Notification
                _timer.Stop();
                _timerStatus = TimerStatus.FINISHED;
                _currentTime = 0;
                //TODO : Better sounds 
                Console.Beep();
                //TODO: Cross Platform Notification
                #if WindoWS
                    Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder builder = new ToastContentBuilder();
                    if (_activityName == ActivityName.SESSION){
                        builder.AddText("Session Finished! Time for a Break!");;
                    }else if(_activityName == ActivityName.BREAK){
                        builder.AddText("Break Finished! Time to Work!");
                    }
                    builder.Show();
                #endif
                
                // complete Current Session in Queue
                if (_activityName == ActivityName.SESSION)
                {
                    CompleteCurrentInQueue();
                }

                //Change Activity
                _activityName = _activityName == ActivityName.SESSION ? ActivityName.BREAK : ActivityName.SESSION;

                //Wait for 3 seconds before starting the next session
                await Task.Delay(3000);


                _currentTime = _activityName == ActivityName.SESSION ? _sessionTime : _breakTime;
                if (_activityName == ActivityName.SESSION)
                {
                    SetNextInQueue();
                }


                _timer.Start();
                _timerStatus = TimerStatus.RUNNING;
            }
        };
        if (timerTime != 0)
        {
            _timer.Start();
            _timerStatus = TimerStatus.RUNNING;
        }
    }

    static (string, string, string, string) ParseCommand(string command)
    {
        command = command.Trim();
        string[] commands = command.Split(' ');
        string commandName = commands[0].ToLower();
        string firstArg = commands.Length > 1 ? commands[1] : "";
        string secondArg = commands.Length > 2 ? commands[2] : "";
        string thirdArg = commands.Length > 3 ? commands[3] : "";
        return (commandName, firstArg, secondArg, thirdArg);
    }

    static void Quit()
    {
        Console.Clear();
        CursorToHome();
        _timer.Dispose();
        _messageTimer.Dispose();
        // TODO: UPDATE JSON FILE
    }

    static void SaveQueues(string path)
    {
        ProjectSpace p = new ProjectSpace { ProjectName = _projectName, Queue = _pomodoroList }; 
        string jsonString = JsonSerializer.Serialize(p, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, jsonString); 
    }

    static void LoadQueues(string path)
    {
        string json = File.ReadAllText(path);
        ProjectSpace? newP = JsonSerializer.Deserialize<ProjectSpace>(json, new JsonSerializerOptions { WriteIndented = true });
        if (newP == null)
        {
            _message = "Cannot read File or it is empty";
            return;
        }
        _projectName = newP.ProjectName;
        _pomodoroList.AddRange(newP.Queue);
        SetNextInQueue(); 
    }

static void ProcessCommand(string command)
    {
        var(commandName, firstArg, secondArg, thirdArg) = ParseCommand(command);
        
        switch (commandName)
        {
            case "quit":
                _running = false;
                break;
            case "stop":
                _timer.Stop();
                _timerStatus = TimerStatus.PAUSED;
                break;
            case "start":
                if(_timerStatus == TimerStatus.FINISHED)
                { SetNextInQueue(); }
                _timer.Start();
                _timerStatus = TimerStatus.RUNNING;
                break;
            case "break":
                _timer.Stop();
                _breakTime = int.TryParse(firstArg, out var time) ?  time * 60 : _breakTime;
                _currentTime = _breakTime;
                _activityName = ActivityName.BREAK;
                _timer.Start();
                _timerStatus = TimerStatus.RUNNING;
                break;
            case "session":
                _timer.Stop();
                _timerStatus = TimerStatus.PAUSED;
                _sessionTime = int.TryParse(firstArg, out var newSessionTime) ?  newSessionTime * 60 : _sessionTime;
                _currentTime = _sessionTime;
                _activityName = ActivityName.SESSION;
                _timer.Start();
                _timerStatus = TimerStatus.RUNNING;
                break;
            case "reset":
                _timer.Stop();
                _timerStatus = TimerStatus.PAUSED;
                _currentTime = _activityName == ActivityName.SESSION ? _sessionTime : _breakTime;
                break;
            case "add":
                if (firstArg == "")
                {
                    _message = "Please enter a name for the session";
                    break;   
                }
                int numberOfSessions = ((secondArg != "") && int.TryParse(secondArg, out  numberOfSessions)) ? numberOfSessions : 1;
                int timePerSession = (thirdArg != "") && int.TryParse(thirdArg, out  timePerSession) ? timePerSession * 60 : _sessionTime;
                _pomodoroList.Add(new PomodoroItem(firstArg, timePerSession, numberOfSessions));
                break;
             case "completed":
                 _showCompletedSessions = !_showCompletedSessions;
                 break;
             case "projectdirectory":
                _appConfig.SaveProjectFilePath = firstArg;
                UpdateConfig();
                _message = "Config Updated!";
                 break;
             case "save":
                 _timer.Stop();
                 _timerStatus = TimerStatus.PAUSED;
                 _message = "Saving...";
                 string path = firstArg;
                 if (path != "" && Path.IsPathRooted(path))
                 {
                     if (File.Exists(path))
                     {
                         SaveQueues(path);
                     }
                     else
                     {
                         if (Path.HasExtension(path) && Path.GetExtension(path) == ".json")
                         {
                             File.Create(path).Close();
                             SaveQueues(path);
                         }
                         else
                         {
                             _message = "Something is wrong with provided file path.";
                             break;
                         }
                     }
                 }else if (path != "" && !Path.IsPathRooted(path))
                 {
                     if (_appConfig.SaveProjectFilePath == "")
                     {
                         _message = "Please set a project directory or provide an absolute path";
                         break;
                     }

                     if (!Path.HasExtension(path) || Path.GetExtension(path) != ".json")
                     {
                         path = Path.ChangeExtension(path, ".json");
                     }

                     path = Path.Combine(_appConfig.SaveProjectFilePath, path);
                     if (!File.Exists(path))
                     {
                         File.Create(path).Close();
                     }

                     SaveQueues(path);
                 }else if (path == "")
                 {


                     if (_projectName != "" && _appConfig.SaveProjectFilePath != "")
                     {
                         path = Path.Combine(_appConfig.SaveProjectFilePath, _projectName + ".json");
                         SaveQueues(path);
                     }
                     else if (_projectName == "" && _appConfig.SaveProjectFilePath != "")
                     {
                         path = Path.Combine(_appConfig.SaveProjectFilePath, "default.json");
                         while (File.Exists(path))
                         {
                             path = Path.Combine(_appConfig.SaveProjectFilePath,
                                 $"default{Path.GetRandomFileName()}.json");
                         }

                         File.Create(path).Close();
                         SaveQueues(path);
                     }
                     else if (_projectName == "" && _appConfig.SaveProjectFilePath == "")
                     {
                         _message = "Please set a project directory or provide an absolute path";
                         break;
                     }
                 }
                 _message = "Saved";
                 break;
             case "load":
                 _timer.Stop();
                 _timerStatus = TimerStatus.PAUSED;
                 path = firstArg;
                 if (path == "")
                 {
                     if (_appConfig.SaveProjectFilePath == "" || _projectName == "")
                     {
                         _message = "Please Provide a path";
                         break;
                     }
                     else
                     {
                         path = Path.Combine(_appConfig.SaveProjectFilePath, _projectName + ".json");
                         if (!File.Exists(path))
                         {
                             _message = "File not found";
                             break;
                         }
                         _message = "Loading...";
                        LoadQueues(path);   
                     }
                 }
                 else
                 {
                     if (Path.IsPathRooted(path) && Path.HasExtension(path) && Path.GetExtension(path) == ".json")
                     {

                         if (!File.Exists(path))
                         {
                             _message = "File not found";
                             break;
                         }

                         _message = "Loading...";
                         LoadQueues(path);
                     }
                     else if (!Path.IsPathRooted(path))
                     {
                         if (_appConfig.SaveProjectFilePath == "")
                         {
                             _message = "Please set a project directory or provide an absolute path";
                             break;
                         }

                         if (!Path.HasExtension(path) || Path.GetExtension(path) != ".json")
                         {
                             path = Path.ChangeExtension(path, ".json");
                         }

                         path = Path.Combine(_appConfig.SaveProjectFilePath, path);

                         if (!File.Exists(path))
                         {
                             _message = "File not found or wrong file type";
                             break;
                         }

                         _message = "Loading...";
                         LoadQueues(path);

                     }
                     else
                     {
                         _message = "Invalid Path or File";
                         break;
                     }
                 }
                 _message = "Loaded!";
                 break;
             case "project":
                if (firstArg == "")
                {
                    _message = "Please enter a name for the project";
                    break;
                }
                _projectName = firstArg;
                 break;
            default:
                _message = $"Unknown command: {command}";
                break;
            
        }
        _commandHistory.Add(new Commands() { Name = command });
    }
    static void ProcessKeys()
    {
        if (Console.KeyAvailable)
        {
            ConsoleKeyInfo key = Console.ReadKey(true);
            if ((key.Modifiers & ConsoleModifiers.Control) != 0 && key.Key == ConsoleKey.C)
            {
                _running = false;
            }

            if (key.Key != ConsoleKey.UpArrow && key.Key != ConsoleKey.DownArrow)
            {
                _lastCommandIndex = _commandHistory.Count;
            }

            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    _running = false;
                    break;
                case ConsoleKey.Enter:
                    ProcessCommand(_command);
                    _command = "";
                    break;
                case ConsoleKey.Spacebar:
                    _command += " ";
                    break;
                case ConsoleKey.Backspace:
                    if (_command.Length > 0)
                    {
                        _command = _command.Substring(0, _command.Length - 1);
                    }

                    break;
                case var k when (k >= ConsoleKey.A & k <= ConsoleKey.Z):
                    _command += key.KeyChar;
                    break;
                case var k when (k >= ConsoleKey.D0 & k <= ConsoleKey.D9):
                    _command += $"{key.KeyChar}";
                    break;
                case ConsoleKey.UpArrow:
                    _lastCommandIndex = _lastCommandIndex == 0 ? _commandHistory.Count - 1 : _lastCommandIndex - 1;
                    if (_lastCommandIndex < 0)
                    {
                        _message = "No previous commands";
                        _lastCommandIndex = _commandHistory.Count - 1;
                        break;
                    }
                    _command = _commandHistory[_lastCommandIndex].Name;
                    break;
                case ConsoleKey.DownArrow:
                    _lastCommandIndex = _lastCommandIndex >= _commandHistory.Count - 1 ? 0 : _lastCommandIndex + 1;
                    if (_lastCommandIndex >= _commandHistory.Count)
                    {
                        _message = "No previous commands";
                        _lastCommandIndex = 0;
                        break;
                    }
                    _command = _commandHistory[_lastCommandIndex].Name;
                    break;
                default:
                    _command += key.KeyChar;
                    break;
            }
        }
        
        Console.Write(_command);
    }

    static void ParseInput(ref int timerTime, ref int breakTime)
    {
        string[] arguments = Environment.GetCommandLineArgs();

        for (int i = 1; i < arguments.Length; i++)
        {
            if (i == 1 && int.TryParse(arguments[i], out timerTime))
            {
                timerTime = timerTime * 60;
            }
            else if (arguments[i] == "-S" || arguments[i] == "--session")
            {
                int nextIndex = i + 1;
                int secondIndex = i + 2;
                if (nextIndex < arguments.Length && int.TryParse(arguments[nextIndex], out timerTime))
                {
                    timerTime = timerTime * 60;
                    i = nextIndex;
                }else if (arguments[nextIndex] == "-m" || arguments[nextIndex] == "--minutes")
                {
                    if (secondIndex < arguments.Length && int.TryParse(arguments[secondIndex], out timerTime))
                    {
                        timerTime = timerTime * 60;
                        i = secondIndex;
                    }
                }
                else if (arguments[nextIndex] == "-s" || arguments[nextIndex] == "--seconds")
                {
                    if (secondIndex < arguments.Length && int.TryParse(arguments[secondIndex], out timerTime))
                    {
                        timerTime = timerTime * 1;
                        i = secondIndex;
                    }
                }
            }  
            else if (arguments[i] == "-b" || arguments[i] == "--break")
            {
                int nextIndex = i + 1;
                int secondIndex = i + 2;
                if (nextIndex < arguments.Length && int.TryParse(arguments[nextIndex], out breakTime))
                {
                    breakTime = breakTime * 60;
                    i = nextIndex;
                }else if (arguments[nextIndex] == "-m" || arguments[nextIndex] == "--minutes")
                {
                    if (secondIndex < arguments.Length && int.TryParse(arguments[secondIndex], out breakTime))
                    {
                        breakTime = breakTime * 60;
                        i = secondIndex;
                    }
                }
                else if (arguments[nextIndex] == "-s" || arguments[nextIndex] == "--seconds")
                {
                    if (secondIndex < arguments.Length && int.TryParse(arguments[secondIndex], out breakTime))
                    {
                        breakTime = breakTime * 1;
                        i = secondIndex;
                    }
                }
            }else if (arguments[i] == "-m" || arguments[i] == "--minutes")
            {
                int secondIndex = i + 1;
                if (secondIndex < arguments.Length && int.TryParse(arguments[secondIndex], out timerTime))
                {
                    timerTime = timerTime * 60;
                    i = secondIndex;
                }
            }
            else if (arguments[i] == "-s" || arguments[i] == "--seconds")
            {
                int secondIndex = i + 1;
                if (secondIndex < arguments.Length && int.TryParse(arguments[secondIndex], out timerTime))
                {
                    timerTime = timerTime * 1;
                    i = secondIndex;
                }
            }
        }
    }

    static string GetConfigFilePath()
    {
        string appName = "PomodoroCLI";
        string configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName);
        Directory.CreateDirectory(configDir);
        return Path.Combine(configDir, "config.json");
    }
    static void UpdateConfig()
    {
        string configPath = GetConfigFilePath();
        string json = JsonSerializer.Serialize(_appConfig, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json);
    }
    
    static void ConfigInit()
    {
        string configPath = GetConfigFilePath();
        if (!File.Exists(configPath))
        {
            File.Create(configPath).Close();
            _appConfig = new AppConfig();
            string outText = JsonSerializer.Serialize(_appConfig, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, outText);
        }

        string json = File.ReadAllText(configPath);
            AppConfig? conf = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions { WriteIndented = true });
            if (conf != null)
            {
                _appConfig = conf; 
            }
        
        
    }
    
    static void Main(string[] argv)
    {
        //TODO: Stats feature
        //TODO: SYNC workspace file to track time on road
        int timerTime = 0, breakTime = 0; 
        ConfigInit();
        ParseInput(ref timerTime, ref breakTime);
        TimerInit(timerTime,breakTime);
        MessageTimerInit();        
        while (_running)
        {
            Console.CursorVisible = false;
            RenderUi();
            ProcessKeys();
            ClearFromCursorToEnd();
            Console.CursorVisible = true;
        }
        Quit();
    }
}