# POMODORO CLI APP
##### A CLI Pomodoro timer made using dotnet.
---

## 📖 Purpose

- **Why make it?**
  - To learn dotnet and C#
  - To make a productivity tool that tracks how much time I actually spend being productive
  - To track how much time it takes for me to finish a project

---

## 🚀 Features

### ✅ Completed Features

- **Config support:**  
  Save and load user preferences (session time, break time, project directory, etc.) in a config file.
- **Save and load queues:**  
  Persist your Pomodoro session queue to a file and reload it later.
- **Start the timer** using either minutes or seconds in the command.  
  _Example:_ `dotnet run -m 5` or `dotnet run -s 10`
- **Queue** for upcoming sessions and their time
- **Key processing** for entering commands, cycling through used commands from the command history, and more
- **Message system** for feedback if an error happens
- **Commands system:** Use commands like `quit` or `add` to control the timer

#### Command List

- `add`: Adds a session in the queue with time for session and number of sessions  
  _e.g._ `add testingSession 2 10` adds 2 sessions named `testingSession` of 10 minutes each
- `start`: Starts the paused or next session
- `stop`: Pauses a session or break
- `break`: Sets the break amount and starts it immediately
- `session`: Start a session of given time or default time immediately
- `completed`: See the completed sessions
- `save`: Saves the queues to JSON file
- `load`:  Loads the queues from the JSON file
---

### 🛠️ Planned Features & Bugs

#### Features to Add

- [ ] Add a splash page
- [ ] Add ASCII art timer
- [ ] Better completion art and celebration
- [ ] Add a message queue instead of a single message string
- [ ] Better sounds (cross-platform, custom audio)
- [ ] Cross-platform notification support
- [ ] Stats feature (track total Pomodoros, time spent, streaks, etc.)
- [ ] SYNC workspace file to track time on the road
- [ ] Auto-update JSON file on quit

#### Bugs to Eliminate

- [ ] **Unlimited scrolling when UI resizes and there is no handling of that**  
       _Detect window size changes and re-render or clear the UI accordingly._

---
