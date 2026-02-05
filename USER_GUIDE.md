# Incident Management Agent - User Guide

## Getting Started

### 1. Accessing the Agent
- Navigate to the web application in your browser
- You'll see a modern dark dashboard with "?? Incident Management Agent" in the header
- Your session ID will be displayed (saved for continuity)

### 2. Initial Setup
No setup required! The agent is ready to use immediately. Your session data is automatically persisted in your browser.

## Quick Start Options

### Option A: Use Quick Action Buttons (Recommended for beginners)
Located in the right sidebar, click any button:
- **?? View Open** - See all open incidents at a glance
- **?? Critical Only** - Focus on urgent incidents
- **?? High Priority** - Review high priority items
- **? Resolved** - See recently fixed incidents
- **?? Analytics** - Get incident metrics and trends
- **? Create New** - Start creating a new incident

### Option B: Type Natural Language Commands
Use the text input at the bottom to type commands:
```
Show open incidents
Show critical incidents
Analyze incidents
Resolve incident INC123456
Create incident: Database connection timeout
```

### Option C: Combine Both Methods
1. Click a quick action button
2. Review the results
3. Click a "next action" button that appears below the response
4. Or type a new command for more control

## Common Tasks

### Task 1: View All Open Incidents
**Method A (Quick Button):** Click "?? View Open"
**Method B (Text Command):** Type "Show open incidents"

**Result:** See a list of all open incidents with:
- Status icon (?? Open, ?? In Progress, ?? On Hold, ? Resolved)
- Priority indicator (?? Critical, ?? High, ?? Medium, ?? Low)
- Incident ID (e.g., INC0012345678)
- Title
- Assigned person

### Task 2: Focus on Critical Incidents
**Command:** Click "?? Critical Only" or type "Show critical incidents"

**Result:** See only critical priority incidents, helpful for triage

### Task 3: Get Incident Analytics
**Command:** Click "?? Analytics" or type "Analyze incidents"

**Result:** Comprehensive metrics including:
- Total incidents count
- Open vs. resolved breakdown
- Critical and high priority count
- Average resolution time
- Top incident categories

### Task 4: Create a New Incident
**Method A (Quick Form):**
1. Click "? Create New"
2. Fill in the incident title
3. Select priority (Low, Medium, High, Critical)
4. Select category (Network, Database, Application, etc.)
5. Click "Create Incident"

**Method B (Text Command):**
Type one of these formats:
```
Create incident: Database timeout
Create incident: Network latency priority: high category: Network
Create incident: API crash priority: critical category: Application
```

**Result:** New incident created with auto-generated ID

### Task 5: View Specific Incident Details
**Command:** Type "Show incident details INC001234" or "Get incident INC001234"

**Result:** See full incident information:
- ID
- Title
- Description
- Status
- Priority
- Severity
- Category
- Assigned person
- Creation time
- Resolution time (if resolved)

### Task 6: Change Incident Status
**Commands:**
```
Update incident INC001234 status to In Progress
Change incident INC001234 to On Hold
Resolve incident INC001234
Close incident INC001234
```

**Result:** Incident status updated immediately

### Task 7: Assign Incident to a Team Member
**Command:**
```
Assign incident INC001234 to John Doe
Assign incident INC001234 to john.doe@company.com
```

**Result:** Incident reassigned

### Task 8: View Resolved Incidents
**Command:** Click "? Resolved" or type "Show resolved incidents"

**Result:** See recently resolved incidents (last 10 by default)

## Understanding the Response Format

Each response from the agent includes:

### 1. Main Response Text
The primary answer with incident data, statistics, or confirmation

### 2. Tool Badge
Shows which system processed your request (usually "Incidents")

### 3. Next Action Buttons
Context-aware buttons suggesting logical follow-ups:
```
Example after "Show open incidents":
[View critical incidents] [Assign incident] [Analyze incidents]
```
Click any button to execute that command

## Advanced Commands

### Multi-Condition Queries
```
Show high priority incidents
List all open incidents with critical priority
Get incident analytics by category
```

### Incident Priority Levels
- **Critical** - Requires immediate action
- **High** - Important, needs attention soon
- **Medium** - Regular priority (default)
- **Low** - Can wait, less urgent

### Incident Categories
- **Network** - Connectivity, DNS, routing issues
- **Database** - SQL, connection pools, data issues
- **Application** - App crash, feature failure
- **Server** - Hardware, OS, infrastructure
- **Security** - Access, breach, vulnerability
- **Performance** - Slow response, high latency
- **Other** - Miscellaneous

## Troubleshooting

### Issue: "Server not running" error
**Solution:** Make sure the backend is running:
- Windows: Run `dotnet run` in project directory
- Check that port 5000/5001 is available

### Issue: Chat shows old session data
**Solution:** Clear your chat history:
- Click the "??? Clear Chat" button
- Confirm the action
- A new session will be created

### Issue: Incident creation failing
**Possible causes:**
- Missing required title field
- Invalid date format (if using due dates)
- Try simpler command: "Create incident: My issue"

### Issue: Commands not recognized
**Tips:**
- Use exact keywords: "Show open incidents" (not "Display")
- Include incident IDs with prefix: "INC001234" (not just "001234")
- Capitalize priority: "high" or "High" (both work)

## Tips & Best Practices

### 1. Use Quick Actions for Speed
The sidebar buttons are the fastest way to access common functions. Most users prefer these over typing.

### 2. Follow Suggested Next Actions
The agent provides intelligent suggestions. These buttons lead to common next steps and save typing.

### 3. Keep Commands Simple
The agent understands natural language, but simple commands work best:
- Good: "Show critical incidents"
- Also Good: "Critical incidents only"
- Less ideal: "I want to see the most urgent incidents"

### 4. Use Session ID Reference
Your session ID is shown in the header. Save it if you need to reference your session later.

### 5. Monitor Quick Stats
Check the "?? Quick Stats" panel on the right to see current incident counts at a glance.

### 6. Clear Chat Periodically
Long conversations can become overwhelming. Start fresh with "??? Clear Chat" for a new perspective.

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Enter | Send message |
| Shift+Enter | New line in message (future feature) |
| Ctrl+L | Focus input box |

## Session Management

### What is a Session?
A session is your unique conversation context. It persists between page refreshes and maintains state across multiple commands.

### Viewing Your Session ID
- Located in the header next to "Session ID:"
- Example: `session_abc1234567890...`
- First 12 characters shown for brevity

### Clearing Your Session
- Click "??? Clear Chat"
- Confirm when prompted
- Creates a completely new session
- All incident data remains on server (not deleted)

## Response Times

### Typical Response Times
- Quick actions (list, count): < 1 second
- Analytics: < 2 seconds
- Create incident: < 1 second
- Status updates: < 1 second

All operations display "Processing incident..." while working.

## Error Messages & Solutions

| Message | Meaning | Solution |
|---------|---------|----------|
| "Incident not found" | INC ID doesn't exist | Verify the incident ID from the list |
| "Please provide incident ID" | Missing ID in command | Include INC ID in your command |
| "Server connection error" | Backend not running | Start the backend service |
| "Invalid status" | Wrong status name | Use: Open, In Progress, On Hold, Resolved |

## FAQ

**Q: Can I modify incident data in this agent?**
A: Yes! You can change status, assign incidents, and create new ones.

**Q: Is my data persistent?**
A: Incident data is stored in the backend. Your session data persists in browser localStorage.

**Q: Can multiple people use the same account?**
A: Each user gets their own session ID, so parallel conversations work fine.

**Q: How long does session data persist?**
A: Depends on your browser's localStorage policy, typically until manually cleared.

**Q: Can I export incident data?**
A: Use "Analyze incidents" to get a summary. Export feature coming soon!

**Q: Is there a mobile app?**
A: The web interface is responsive and works on mobile devices.

**Q: Can I integrate with real ServiceNow?**
A: Currently uses in-memory storage. Backend integration can be configured.

**Q: What if I need help?**
A: Check this guide or the incident agent's "About" section for more information.

## Support

For issues or feature requests, provide:
1. Your session ID
2. The command you used
3. The exact error message
4. Steps to reproduce

---

**Last Updated:** 2024
**Version:** 1.0
**Status:** Production Ready
