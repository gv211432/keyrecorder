# Security Policy

## Supported Versions

We release patches for security vulnerabilities for the following versions:

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, please report them via email to: **[security@keyrecorder-project.example]** (replace with actual email)

### What to Include

Please include as much information as possible:

- **Type of vulnerability** - What kind of issue is it? (e.g., SQLi, XSS, privilege escalation)
- **Affected components** - Which parts of the codebase are affected?
- **Attack scenario** - How could an attacker exploit this?
- **Impact assessment** - What could an attacker accomplish?
- **Steps to reproduce** - Detailed steps to reproduce the vulnerability
- **Proof of concept** - Code or screenshots demonstrating the issue (if possible)
- **Suggested fix** - If you have ideas on how to fix it

### Response Timeline

- **Initial Response:** Within 48 hours
- **Status Update:** Within 7 days
- **Fix Timeline:** Varies by severity (see below)

### Severity Levels

#### Critical (24-72 hours)
- Remote code execution
- Privilege escalation to SYSTEM
- Database compromise leading to data exfiltration

#### High (7-14 days)
- Local privilege escalation
- Bypass of security features
- Authentication bypass

#### Medium (30 days)
- Information disclosure
- Denial of service
- Minor data leaks

#### Low (90 days)
- UI spoofing
- Minor information disclosure

## Security Best Practices

### For Users

1. **Run as Non-Admin** - Only install as Administrator, run UI as normal user
2. **Keep Updated** - Install security patches promptly
3. **Secure Database** - Protect `C:\ProgramData\KeyRecorder` with appropriate ACLs
4. **Regular Backups** - Back up your keystroke database regularly
5. **Review Retention** - Don't keep more data than necessary

### For Developers

1. **Input Validation** - Validate all user input
2. **Parameterized Queries** - Use parameterized SQL queries to prevent injection
3. **Least Privilege** - Service runs with minimal required permissions
4. **Secure IPC** - Named Pipes use ACLs for access control
5. **No Network** - KeyRecorder never accesses the network
6. **Code Review** - All PRs reviewed for security issues

## Known Security Considerations

### By Design

These are intentional design decisions, not vulnerabilities:

1. **Administrator Required** - Service installation requires admin rights (by design for Windows Services)
2. **Global Hook** - Keyboard hook captures all keystrokes (this is the core functionality)
3. **Local Storage** - Data stored in plaintext SQLite (encrypted filesystem recommended if needed)

### Mitigations in Place

- **ACL-Protected Named Pipes** - Only authenticated users can connect
- **WAL Mode** - Prevents database corruption
- **Input Sanitization** - All keyboard input is sanitized before storage
- **No Network Access** - Application never transmits data
- **Process Isolation** - Service and UI run in separate processes

## Responsible Disclosure

We follow coordinated vulnerability disclosure:

1. **Report received** - We acknowledge receipt
2. **Investigation** - We investigate and confirm the issue
3. **Fix developed** - We develop and test a fix
4. **Fix released** - We release the patch
5. **Public disclosure** - After 90 days or when patch is released

## Security Updates

Security updates are released as:

- **Patch Releases** - For critical/high severity issues
- **Release Notes** - Include CVE IDs if assigned
- **Security Advisories** - Published on GitHub

## Bug Bounty

We currently do not offer a paid bug bounty program, but we:

- ✅ Credit security researchers in release notes
- ✅ Fast-track fixes for reported vulnerabilities
- ✅ Publicly thank contributors (with permission)

## Hall of Fame

Security researchers who have helped make KeyRecorder more secure:

<!-- Will be updated as vulnerabilities are reported and fixed -->
- *No vulnerabilities reported yet*

## Contact

For security-related questions:

- **Email:** [security@keyrecorder-project.example] (replace with actual)
- **PGP Key:** [Link to PGP key] (if applicable)

For general bugs (non-security):
- **GitHub Issues:** [../../issues](../../issues)

---

**We take security seriously.** Thank you for helping keep KeyRecorder and its users safe!
