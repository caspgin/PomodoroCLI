# A CLI Pomodoro timer made using dotnet.

## **Why make it?**
1. To learn dotnet and c#
2. To make a productivity tool that tracks how much time I actually spend being productive
3. To track how much time it takes for me to finish a project.

## Features
1. Start the timer using either minutes or seconds in the command. eg. "dotnet run -m 5" or "dotnet run -s 10"
2. Queue for upcoming sessions and their time
3. key processing like for entering commands, cycling through used commands from the command history and more
4. Message system for feedback if an error happens.
5. Commands system: Use commands like "quit" or "add" to control timer. Following is the list of Commands
    1. add: Adds a session in the queue with time for session and number of sessions e.g. "add testingSession 2 10" adds 2 sessions name testingSession of 10 minutes each
    2. Start: Starts the paused or next session
    3. Stop: Pauses a session or break
    4. break: Sets the break amount and starts it immediately
    5. session: Start a session of given time or default time immedaiately
    6. completed: See the completed ssessions

   
