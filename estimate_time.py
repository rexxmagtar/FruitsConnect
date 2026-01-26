import subprocess
from datetime import datetime

def get_git_log():
    cmd = 'git log --pretty=format:"%ad" --date=iso'
    result = subprocess.run(cmd, capture_output=True, text=True, shell=True)
    return result.stdout.strip().split('\n')

def estimate_time(log_dates, max_gap_hours=2, session_buffer_minutes=30):
    if not log_dates:
        return 0
    
    dates = [datetime.fromisoformat(d) for d in log_dates]
    dates.sort()
    
    total_seconds = 0
    if not dates:
        return 0
    
    current_session_start = dates[0]
    current_session_last = dates[0]
    
    sessions = []
    
    for i in range(1, len(dates)):
        gap = (dates[i] - current_session_last).total_seconds() / 3600
        if gap > max_gap_hours:
            # End current session
            session_duration = (current_session_last - current_session_start).total_seconds()
            # Add buffer for the session (time spent before first commit or for single commit)
            session_duration += session_buffer_minutes * 60
            sessions.append(session_duration)
            
            # Start new session
            current_session_start = dates[i]
            current_session_last = dates[i]
        else:
            current_session_last = dates[i]
            
    # Add the last session
    session_duration = (current_session_last - current_session_start).total_seconds()
    session_duration += session_buffer_minutes * 60
    sessions.append(session_duration)
    
    total_hours = sum(sessions) / 3600
    return total_hours, len(sessions)

def count_lines():
    cmd = 'git ls-files'
    result = subprocess.run(cmd, capture_output=True, text=True, shell=True)
    files = result.stdout.strip().split('\n')
    
    total_lines = 0
    code_files = 0
    for f in files:
        if f.endswith(('.cs', '.py', '.js', '.json', '.html', '.css')):
            code_files += 1
            try:
                with open(f, 'r', encoding='utf-8', errors='ignore') as file:
                    total_lines += len(file.readlines())
            except:
                pass
    return code_files, total_lines

if __name__ == "__main__":
    log_dates = get_git_log()
    hours, session_count = estimate_time(log_dates)
    code_files, total_lines = count_lines()
    
    print(f"Total estimated development hours: {hours:.2f}")
    print(f"Number of work sessions: {session_count}")
    print(f"Number of code files: {code_files}")
    print(f"Total lines of code: {total_lines}")
    print(f"Average hours per session: {hours/session_count:.2f}")
    print(f"Total commits: {len(log_dates)}")
