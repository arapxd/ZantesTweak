using System.Collections.Generic;

namespace ZantesEngine.Services
{
    public enum TweakRisk
    {
        Safe,
        Caution
    }

    public sealed class SystemTweakDefinition
    {
        public required string Key { get; init; }
        public required string Title { get; init; }
        public required string Category { get; init; }
        public string Description { get; init; } = string.Empty;
        public TweakRisk Risk { get; init; } = TweakRisk.Safe;
        public string Warning { get; init; } = string.Empty;
        public bool Recommended { get; init; }
        public bool RequiresRestart { get; init; }
        public required IReadOnlyList<string> Commands { get; init; }
    }

    public static class SystemTweakCatalog
    {
        public static readonly IReadOnlyDictionary<string, SystemTweakDefinition> All =
            new Dictionary<string, SystemTweakDefinition>
            {
                ["power_high_performance"] = new()
                {
                    Key = "power_high_performance",
                    Title = "High Performance Power Plan",
                    Category = "System",
                    Description = "Activates high performance power profile.",
                    Recommended = true,
                    Commands = new[] { "powercfg /setactive SCHEME_MIN" }
                },
                ["disable_hibernate"] = new()
                {
                    Key = "disable_hibernate",
                    Title = "Disable Hibernation",
                    Category = "System",
                    Description = "Disables hibernation to free disk space.",
                    Risk = TweakRisk.Caution,
                    Warning = "Laptop sleep-hibernate workflow may change.",
                    Commands = new[] { "powercfg /hibernate off" }
                },
                ["disable_startup_delay"] = new()
                {
                    Key = "disable_startup_delay",
                    Title = "Disable Startup Delay",
                    Category = "System",
                    Description = "Reduces startup app delay.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize\" /v StartupDelayInMSec /t REG_DWORD /d 0 /f"
                    }
                },
                ["enable_taskbar_end_task"] = new()
                {
                    Key = "enable_taskbar_end_task",
                    Title = "Enable Taskbar End Task",
                    Category = "System",
                    Description = "Adds quick End Task action to taskbar app menu (Windows 11).",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\TaskbarDeveloperSettings\" /v TaskbarEndTask /t REG_DWORD /d 1 /f"
                    }
                },
                ["disable_fast_startup"] = new()
                {
                    Key = "disable_fast_startup",
                    Title = "Disable Fast Startup",
                    Category = "System",
                    Description = "Disables hybrid boot startup mode.",
                    Risk = TweakRisk.Caution,
                    Warning = "Boot behavior may change on some systems.",
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power\" /v HiberbootEnabled /t REG_DWORD /d 0 /f"
                    }
                },
                ["disable_dynamic_tick"] = new()
                {
                    Key = "disable_dynamic_tick",
                    Title = "Disable Dynamic Tick",
                    Category = "System",
                    Description = "Disables dynamic tick timer behavior for steadier scheduling.",
                    Risk = TweakRisk.Caution,
                    Warning = "May increase idle power usage on some machines.",
                    Commands = new[]
                    {
                        "bcdedit /set disabledynamictick yes"
                    }
                },
                ["disable_connected_standby"] = new()
                {
                    Key = "disable_connected_standby",
                    Title = "Disable Connected Standby",
                    Category = "System",
                    Description = "Disables Modern Standby style low-power connected sleep path.",
                    Risk = TweakRisk.Caution,
                    Warning = "Sleep behavior can change on laptops and modern mobile chipsets.",
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\" /v PlatformAoAcOverride /t REG_DWORD /d 0 /f"
                    }
                },
                ["disable_remote_assistance"] = new()
                {
                    Key = "disable_remote_assistance",
                    Title = "Disable Remote Assistance",
                    Category = "System",
                    Description = "Turns off Remote Assistance request support.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Remote Assistance\" /v fAllowToGetHelp /t REG_DWORD /d 0 /f"
                    }
                },
                ["cpu_minimum_state_100"] = new()
                {
                    Key = "cpu_minimum_state_100",
                    Title = "CPU Minimum State 100%",
                    Category = "System",
                    Description = "Sets active plan minimum processor state to 100% on AC.",
                    Risk = TweakRisk.Caution,
                    Warning = "Can increase heat and power usage on laptops.",
                    Commands = new[]
                    {
                        "powercfg /setacvalueindex scheme_current sub_processor PROCTHROTTLEMIN 100",
                        "powercfg /setactive scheme_current"
                    }
                },
                ["cpu_maximum_state_100"] = new()
                {
                    Key = "cpu_maximum_state_100",
                    Title = "CPU Maximum State 100%",
                    Category = "System",
                    Description = "Ensures active plan maximum processor state is 100% on AC.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "powercfg /setacvalueindex scheme_current sub_processor PROCTHROTTLEMAX 100",
                        "powercfg /setactive scheme_current"
                    }
                },
                ["cpu_core_parking_off"] = new()
                {
                    Key = "cpu_core_parking_off",
                    Title = "CPU Core Parking Off (AC)",
                    Category = "System",
                    Description = "Keeps logical processors unparked in active power plan.",
                    Risk = TweakRisk.Caution,
                    Warning = "Can increase temperature and idle power draw.",
                    Commands = new[]
                    {
                        "powercfg /setacvalueindex scheme_current sub_processor CPMINCORES 100 || exit /b 0",
                        "powercfg /setacvalueindex scheme_current sub_processor CPMAXCORES 100 || exit /b 0",
                        "powercfg /setactive scheme_current"
                    }
                },
                ["visualfx_performance"] = new()
                {
                    Key = "visualfx_performance",
                    Title = "Visual Effects Performance Mode",
                    Category = "System",
                    Description = "Optimizes visuals for lower latency.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects\" /v VisualFXSetting /t REG_DWORD /d 2 /f"
                    }
                },
                ["disable_power_throttling"] = new()
                {
                    Key = "disable_power_throttling",
                    Title = "Disable Power Throttling",
                    Category = "System",
                    Description = "Stops automatic throttling for background tasks.",
                    Risk = TweakRisk.Caution,
                    Warning = "Can increase battery and power usage.",
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerThrottling\" /v PowerThrottlingOff /t REG_DWORD /d 1 /f"
                    }
                },
                ["ntfs_disable_last_access"] = new()
                {
                    Key = "ntfs_disable_last_access",
                    Title = "Disable NTFS Last Access",
                    Category = "System",
                    Description = "Reduces metadata write load on disk.",
                    Risk = TweakRisk.Caution,
                    Warning = "Some audit tools may expect last-access tracking.",
                    Commands = new[] { "fsutil behavior set disablelastaccess 1" }
                },
                ["disable_mouse_accel"] = new()
                {
                    Key = "disable_mouse_accel",
                    Title = "Disable Mouse Acceleration",
                    Category = "Gaming",
                    Description = "For consistent raw-like pointer movement.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Control Panel\\Mouse\" /v MouseSpeed /t REG_SZ /d 0 /f",
                        "reg add \"HKCU\\Control Panel\\Mouse\" /v MouseThreshold1 /t REG_SZ /d 0 /f",
                        "reg add \"HKCU\\Control Panel\\Mouse\" /v MouseThreshold2 /t REG_SZ /d 0 /f"
                    }
                },
                ["enable_game_mode"] = new()
                {
                    Key = "enable_game_mode",
                    Title = "Enable Windows Game Mode",
                    Category = "Gaming",
                    Description = "Enables Game Mode optimization path.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v AutoGameModeEnabled /t REG_DWORD /d 1 /f",
                        "reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v AllowAutoGameMode /t REG_DWORD /d 1 /f"
                    }
                },
                ["disable_game_dvr"] = new()
                {
                    Key = "disable_game_dvr",
                    Title = "Disable Game DVR",
                    Category = "Gaming",
                    Description = "Disables game capture overhead.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR\" /v AppCaptureEnabled /t REG_DWORD /d 0 /f",
                        "reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_Enabled /t REG_DWORD /d 0 /f"
                    }
                },
                ["hw_scheduling"] = new()
                {
                    Key = "hw_scheduling",
                    Title = "Hardware GPU Scheduling",
                    Category = "Gaming",
                    Description = "Enables GPU hardware scheduling mode.",
                    Risk = TweakRisk.Caution,
                    Warning = "Some driver versions can behave inconsistently.",
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v HwSchMode /t REG_DWORD /d 2 /f"
                    }
                },
                ["priority_separation"] = new()
                {
                    Key = "priority_separation",
                    Title = "Foreground Priority Separation",
                    Category = "Gaming",
                    Description = "Boosts foreground scheduling behavior.",
                    Risk = TweakRisk.Caution,
                    Warning = "Heavy multitasking can become less smooth.",
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl\" /v Win32PrioritySeparation /t REG_DWORD /d 38 /f"
                    }
                },
                ["disable_sticky_keys_shortcut"] = new()
                {
                    Key = "disable_sticky_keys_shortcut",
                    Title = "Disable Sticky Keys Shortcut",
                    Category = "Gaming",
                    Description = "Prevents accidental hotkey popup during play.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Control Panel\\Accessibility\\StickyKeys\" /v Flags /t REG_SZ /d 506 /f"
                    }
                },
                ["valorant_vanguard_compat"] = new()
                {
                    Key = "valorant_vanguard_compat",
                    Title = "Valorant Vanguard Compatibility",
                    Category = "Gaming",
                    Description = "Applies Vanguard service startup defaults for stable anti-cheat startup.",
                    Risk = TweakRisk.Caution,
                    Warning = "Only affects systems where Riot Vanguard is installed.",
                    Commands = new[]
                    {
                        "sc query vgc >nul 2>&1 && sc config vgc start= demand || exit /b 0",
                        "sc query vgk >nul 2>&1 && sc config vgk start= system || exit /b 0"
                    }
                },
                ["disable_network_throttling"] = new()
                {
                    Key = "disable_network_throttling",
                    Title = "Disable Network Throttling",
                    Category = "Gaming",
                    Description = "Removes multimedia network throttle limit.",
                    Risk = TweakRisk.Caution,
                    Warning = "Can affect stream/download fairness on busy networks.",
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v NetworkThrottlingIndex /t REG_DWORD /d 4294967295 /f"
                    }
                },
                ["mmcss_system_responsiveness"] = new()
                {
                    Key = "mmcss_system_responsiveness",
                    Title = "MMCSS System Responsiveness 10",
                    Category = "Gaming",
                    Description = "Adjusts system responsiveness for game foreground.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v SystemResponsiveness /t REG_DWORD /d 10 /f"
                    }
                },
                ["mmcss_games_task_profile"] = new()
                {
                    Key = "mmcss_games_task_profile",
                    Title = "MMCSS Games Task Profile High",
                    Category = "Gaming",
                    Description = "Sets high-priority MMCSS task profile for Games category.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"GPU Priority\" /t REG_DWORD /d 8 /f",
                        "reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"Priority\" /t REG_DWORD /d 6 /f",
                        "reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"Scheduling Category\" /t REG_SZ /d High /f",
                        "reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"SFIO Priority\" /t REG_SZ /d High /f"
                    }
                },
                ["usb_selective_suspend_off"] = new()
                {
                    Key = "usb_selective_suspend_off",
                    Title = "USB Selective Suspend Off (AC)",
                    Category = "Gaming",
                    Description = "Disables USB selective suspend on AC power for stable input devices.",
                    Risk = TweakRisk.Caution,
                    Warning = "Can increase power usage on laptops.",
                    Commands = new[]
                    {
                        "powercfg /setacvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0",
                        "powercfg /setactive scheme_current"
                    }
                },
                ["disable_mpo"] = new()
                {
                    Key = "disable_mpo",
                    Title = "Disable Multiplane Overlay (MPO)",
                    Category = "Gaming",
                    Description = "Disables MPO to reduce stutter/flicker on some systems.",
                    Risk = TweakRisk.Caution,
                    Warning = "May affect HDR/windowed presentation behavior on some setups.",
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\Dwm\" /v OverlayTestMode /t REG_DWORD /d 5 /f"
                    }
                },
                ["tcp_autotune"] = new()
                {
                    Key = "tcp_autotune",
                    Title = "TCP Auto-Tuning Normal",
                    Category = "Network",
                    Description = "Applies recommended TCP tuning mode.",
                    Recommended = true,
                    Commands = new[] { "netsh int tcp set global autotuninglevel=normal" }
                },
                ["network_rss"] = new()
                {
                    Key = "network_rss",
                    Title = "Enable Receive Side Scaling",
                    Category = "Network",
                    Description = "Uses multiple CPU cores for packet processing.",
                    Recommended = true,
                    Commands = new[] { "netsh int tcp set global rss=enabled" }
                },
                ["tcp_timestamps_disabled"] = new()
                {
                    Key = "tcp_timestamps_disabled",
                    Title = "Disable TCP Timestamps",
                    Category = "Network",
                    Description = "Disables TCP timestamps for lower overhead.",
                    Risk = TweakRisk.Caution,
                    Warning = "May reduce compatibility on uncommon networks.",
                    Commands = new[] { "netsh int tcp set global timestamps=disabled" }
                },
                ["tcp_ecn_disabled"] = new()
                {
                    Key = "tcp_ecn_disabled",
                    Title = "Disable TCP ECN",
                    Category = "Network",
                    Description = "Turns off Explicit Congestion Notification.",
                    Risk = TweakRisk.Caution,
                    Warning = "Can reduce performance on ECN-optimized networks.",
                    Commands = new[] { "netsh int tcp set global ecncapability=disabled" }
                },
                ["tcp_heuristics_disabled"] = new()
                {
                    Key = "tcp_heuristics_disabled",
                    Title = "Disable TCP Heuristics",
                    Category = "Network",
                    Description = "Prevents Windows autotuning heuristics override.",
                    Recommended = true,
                    Commands = new[] { "netsh int tcp set heuristics disabled" }
                },
                ["tcp_chimney_disabled"] = new()
                {
                    Key = "tcp_chimney_disabled",
                    Title = "Disable TCP Chimney Offload",
                    Category = "Network",
                    Description = "Disables legacy chimney offload path.",
                    Risk = TweakRisk.Caution,
                    Warning = "Some older NIC drivers may behave differently.",
                    Commands = new[] { "netsh int tcp set global chimney=disabled" }
                },
                ["tcp_rsc_disabled"] = new()
                {
                    Key = "tcp_rsc_disabled",
                    Title = "Disable TCP RSC",
                    Category = "Network",
                    Description = "Disables Receive Segment Coalescing for lower latency.",
                    Risk = TweakRisk.Caution,
                    Warning = "Can increase CPU usage on slower systems.",
                    Commands = new[] { "netsh int tcp set global rsc=disabled" }
                },
                ["dns_flush"] = new()
                {
                    Key = "dns_flush",
                    Title = "Flush DNS Cache",
                    Category = "Network",
                    Description = "Clears stale DNS cache entries.",
                    Recommended = true,
                    Commands = new[] { "ipconfig /flushdns" }
                },
                ["disable_delivery_opt"] = new()
                {
                    Key = "disable_delivery_opt",
                    Title = "Disable Delivery Optimization",
                    Category = "Network",
                    Description = "Limits peer update bandwidth behavior.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DeliveryOptimization\\Config\" /v DODownloadMode /t REG_DWORD /d 0 /f"
                    }
                },
                ["prefer_ipv4_over_ipv6"] = new()
                {
                    Key = "prefer_ipv4_over_ipv6",
                    Title = "Prefer IPv4 over IPv6",
                    Category = "Network",
                    Description = "Sets system preference to IPv4 when both stacks are available.",
                    Risk = TweakRisk.Caution,
                    Warning = "Can affect IPv6-only network scenarios.",
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters\" /v DisabledComponents /t REG_DWORD /d 32 /f"
                    }
                },
                ["winsock_reset"] = new()
                {
                    Key = "winsock_reset",
                    Title = "Winsock Reset",
                    Category = "Network",
                    Description = "Resets socket stack to defaults.",
                    Risk = TweakRisk.Caution,
                    Warning = "Network reconnect and restart may be required.",
                    RequiresRestart = true,
                    Commands = new[] { "netsh winsock reset" }
                },
                ["disable_telemetry"] = new()
                {
                    Key = "disable_telemetry",
                    Title = "Telemetry Minimal Policy",
                    Category = "Privacy",
                    Description = "Sets data collection to minimum policy.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection\" /v AllowTelemetry /t REG_DWORD /d 0 /f"
                    }
                },
                ["disable_windows_tips"] = new()
                {
                    Key = "disable_windows_tips",
                    Title = "Disable Windows Tips",
                    Category = "Privacy",
                    Description = "Turns off tips and suggestion popups.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v SoftLandingEnabled /t REG_DWORD /d 0 /f",
                        "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v SubscribedContent-338389Enabled /t REG_DWORD /d 0 /f"
                    }
                },
                ["disable_feedback_notifications"] = new()
                {
                    Key = "disable_feedback_notifications",
                    Title = "Disable Feedback Notifications",
                    Category = "Privacy",
                    Description = "Reduces feedback reminder prompts.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Software\\Microsoft\\Siuf\\Rules\" /v NumberOfSIUFInPeriod /t REG_DWORD /d 0 /f"
                    }
                },
                ["disable_activity_history"] = new()
                {
                    Key = "disable_activity_history",
                    Title = "Disable Activity History",
                    Category = "Privacy",
                    Description = "Stops Windows from publishing activity history.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v PublishUserActivities /t REG_DWORD /d 0 /f",
                        "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v UploadUserActivities /t REG_DWORD /d 0 /f"
                    }
                },
                ["disable_clipboard_history"] = new()
                {
                    Key = "disable_clipboard_history",
                    Title = "Disable Clipboard History",
                    Category = "Privacy",
                    Description = "Turns off cloud/extended clipboard history features.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v AllowClipboardHistory /t REG_DWORD /d 0 /f",
                        "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v AllowCrossDeviceClipboard /t REG_DWORD /d 0 /f"
                    }
                },
                ["disable_search_web_results"] = new()
                {
                    Key = "disable_search_web_results",
                    Title = "Disable Search Web Results",
                    Category = "Privacy",
                    Description = "Limits Start menu search to local results.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Software\\Policies\\Microsoft\\Windows\\Explorer\" /v DisableSearchBoxSuggestions /t REG_DWORD /d 1 /f"
                    }
                },
                ["disable_powershell_telemetry"] = new()
                {
                    Key = "disable_powershell_telemetry",
                    Title = "Disable PowerShell Telemetry",
                    Category = "Privacy",
                    Description = "Sets PowerShell telemetry opt-out environment value.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment\" /v POWERSHELL_TELEMETRY_OPTOUT /t REG_SZ /d 1 /f"
                    }
                },
                ["disable_background_apps"] = new()
                {
                    Key = "disable_background_apps",
                    Title = "Disable Background Apps",
                    Category = "Privacy",
                    Description = "Stops UWP background activity.",
                    Risk = TweakRisk.Caution,
                    Warning = "Some app notifications may stop.",
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications\" /v GlobalUserDisabled /t REG_DWORD /d 1 /f"
                    }
                },
                ["disable_xbox_services"] = new()
                {
                    Key = "disable_xbox_services",
                    Title = "Disable Xbox Services",
                    Category = "Services",
                    Description = "Stops Xbox related background services.",
                    Risk = TweakRisk.Caution,
                    Warning = "Xbox app and game pass features may stop.",
                    Commands = new[]
                    {
                        "sc stop XblAuthManager",
                        "sc config XblAuthManager start= disabled",
                        "sc stop XblGameSave",
                        "sc config XblGameSave start= disabled",
                        "sc stop XboxGipSvc",
                        "sc config XboxGipSvc start= disabled"
                    }
                },
                ["disable_diagtrack_service"] = new()
                {
                    Key = "disable_diagtrack_service",
                    Title = "Disable Diagnostic Tracking",
                    Category = "Services",
                    Description = "Stops telemetry tracking related services.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "sc query DiagTrack >nul 2>&1 && (sc stop DiagTrack & sc config DiagTrack start= disabled) || exit /b 0",
                        "sc query dmwappushservice >nul 2>&1 && (sc stop dmwappushservice & sc config dmwappushservice start= disabled) || exit /b 0"
                    }
                },
                ["disable_windows_error_reporting"] = new()
                {
                    Key = "disable_windows_error_reporting",
                    Title = "Disable Windows Error Reporting",
                    Category = "Services",
                    Description = "Disables background error report service uploads.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "sc query WerSvc >nul 2>&1 && (sc stop WerSvc & sc config WerSvc start= disabled) || exit /b 0"
                    }
                },
                ["nvidia_disable_telemetry"] = new()
                {
                    Key = "nvidia_disable_telemetry",
                    Title = "NVIDIA Telemetry Service Off",
                    Category = "Services",
                    Description = "Disables NVIDIA telemetry service when present.",
                    Risk = TweakRisk.Caution,
                    Warning = "Some NVIDIA experience features may be limited.",
                    Commands = new[]
                    {
                        "sc query NvTelemetryContainer >nul 2>&1 && (sc stop NvTelemetryContainer & sc config NvTelemetryContainer start= disabled) || exit /b 0"
                    }
                },
                ["nvidia_clean_shader_cache"] = new()
                {
                    Key = "nvidia_clean_shader_cache",
                    Title = "NVIDIA Shader Cache Cleanup",
                    Category = "Maintenance",
                    Description = "Cleans NVIDIA DX/GL shader cache folders.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "if exist \"%LocalAppData%\\NVIDIA\\DXCache\" del /f /s /q \"%LocalAppData%\\NVIDIA\\DXCache\\*\"",
                        "if exist \"%LocalAppData%\\NVIDIA\\GLCache\" del /f /s /q \"%LocalAppData%\\NVIDIA\\GLCache\\*\""
                    }
                },
                ["fivem_cache_cleanup"] = new()
                {
                    Key = "fivem_cache_cleanup",
                    Title = "FiveM Cache Cleanup",
                    Category = "Maintenance",
                    Description = "Cleans safe local FiveM cache folders without touching downloaded server assets.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "if exist \"%LocalAppData%\\FiveM\\FiveM.app\\data\\cache\\browser\" del /f /s /q \"%LocalAppData%\\FiveM\\FiveM.app\\data\\cache\\browser\\*\"",
                        "if exist \"%LocalAppData%\\FiveM\\FiveM.app\\data\\cache\\db\" del /f /s /q \"%LocalAppData%\\FiveM\\FiveM.app\\data\\cache\\db\\*\"",
                        "if exist \"%LocalAppData%\\FiveM\\FiveM.app\\data\\cache\\priv\" del /f /s /q \"%LocalAppData%\\FiveM\\FiveM.app\\data\\cache\\priv\\*\""
                    }
                },
                ["fivem_logs_cleanup"] = new()
                {
                    Key = "fivem_logs_cleanup",
                    Title = "FiveM Logs & Crash Cleanup",
                    Category = "Maintenance",
                    Description = "Cleans FiveM logs and crash reports.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "if exist \"%LocalAppData%\\FiveM\\FiveM.app\\logs\" del /f /s /q \"%LocalAppData%\\FiveM\\FiveM.app\\logs\\*\"",
                        "if exist \"%LocalAppData%\\FiveM\\FiveM.app\\crashes\" del /f /s /q \"%LocalAppData%\\FiveM\\FiveM.app\\crashes\\*\""
                    }
                },
                ["fivem_nui_storage_reset"] = new()
                {
                    Key = "fivem_nui_storage_reset",
                    Title = "FiveM NUI Storage Reset",
                    Category = "Maintenance",
                    Description = "Resets FiveM NUI local storage for UI cache issues.",
                    Risk = TweakRisk.Caution,
                    Warning = "May sign you out from some server UI integrations.",
                    Commands = new[]
                    {
                        "if exist \"%LocalAppData%\\FiveM\\FiveM.app\\data\\nui-storage\" del /f /s /q \"%LocalAppData%\\FiveM\\FiveM.app\\data\\nui-storage\\*\""
                    }
                },
                ["disable_maps_broker"] = new()
                {
                    Key = "disable_maps_broker",
                    Title = "Disable Downloaded Maps Service",
                    Category = "Services",
                    Description = "Stops offline maps background service.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "sc query MapsBroker >nul 2>&1 && (sc stop MapsBroker & sc config MapsBroker start= disabled) || exit /b 0"
                    }
                },
                ["disable_location_service"] = new()
                {
                    Key = "disable_location_service",
                    Title = "Disable Location Service",
                    Category = "Services",
                    Description = "Disables location service to reduce background sensors.",
                    Risk = TweakRisk.Caution,
                    Warning = "Apps that use location may stop working correctly.",
                    Commands = new[]
                    {
                        "sc query lfsvc >nul 2>&1 && (sc stop lfsvc & sc config lfsvc start= disabled) || exit /b 0"
                    }
                },
                ["disable_print_spooler"] = new()
                {
                    Key = "disable_print_spooler",
                    Title = "Disable Print Spooler",
                    Category = "Services",
                    Description = "Stops print spooler service if printing is not needed.",
                    Risk = TweakRisk.Caution,
                    Warning = "Printing and some PDF virtual printers may stop working.",
                    Commands = new[]
                    {
                        "sc query Spooler >nul 2>&1 && (sc stop Spooler & sc config Spooler start= disabled) || exit /b 0"
                    }
                },
                ["disable_sysmain"] = new()
                {
                    Key = "disable_sysmain",
                    Title = "Disable SysMain Service",
                    Category = "Services",
                    Description = "Reduces background disk activity.",
                    Risk = TweakRisk.Caution,
                    Warning = "App preloading behavior will change.",
                    Commands = new[]
                    {
                        "sc stop SysMain",
                        "sc config SysMain start= disabled"
                    }
                },
                ["disable_ndu_service"] = new()
                {
                    Key = "disable_ndu_service",
                    Title = "Disable NDU Service",
                    Category = "Services",
                    Description = "Disables Windows Network Data Usage monitoring service.",
                    Risk = TweakRisk.Caution,
                    Warning = "Some Windows data usage counters may stop reporting correctly.",
                    Commands = new[]
                    {
                        "sc query Ndu >nul 2>&1 && (sc stop Ndu & sc config Ndu start= disabled) || reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Ndu\" /v Start /t REG_DWORD /d 4 /f"
                    }
                },
                ["disable_search_index"] = new()
                {
                    Key = "disable_search_index",
                    Title = "Disable Search Indexer",
                    Category = "Services",
                    Description = "Disables indexing for less background load.",
                    Risk = TweakRisk.Caution,
                    Warning = "File search can become slower.",
                    Commands = new[]
                    {
                        "sc stop WSearch",
                        "sc config WSearch start= disabled"
                    }
                },
                ["disable_transparency"] = new()
                {
                    Key = "disable_transparency",
                    Title = "Disable Transparency",
                    Category = "System",
                    Description = "Turns off UI transparency effects.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v EnableTransparency /t REG_DWORD /d 0 /f"
                    }
                },
                ["clean_temp"] = new()
                {
                    Key = "clean_temp",
                    Title = "Clean Temp Files",
                    Category = "Maintenance",
                    Description = "Cleans temporary folders.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "del /f /s /q \"%TEMP%\\*\"",
                        "for /d %p in (\"%TEMP%\\*\") do @rd /s /q \"%p\""
                    }
                },
                ["clear_prefetch"] = new()
                {
                    Key = "clear_prefetch",
                    Title = "Clear Prefetch",
                    Category = "Maintenance",
                    Description = "Cleans old prefetch entries.",
                    Risk = TweakRisk.Caution,
                    Warning = "First launch after cleanup can be slower.",
                    Commands = new[]
                    {
                        "del /f /q \"C:\\Windows\\Prefetch\\*\""
                    }
                },
                ["disable_menu_show_delay"] = new()
                {
                    Key = "disable_menu_show_delay",
                    Title = "Disable Menu Show Delay",
                    Category = "System",
                    Description = "Sets desktop menu animation delay to zero.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Control Panel\\Desktop\" /v MenuShowDelay /t REG_SZ /d 0 /f"
                    }
                },
                ["disable_window_animations"] = new()
                {
                    Key = "disable_window_animations",
                    Title = "Disable Window Animations",
                    Category = "System",
                    Description = "Disables minimize/maximize animation transitions.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Control Panel\\Desktop\\WindowMetrics\" /v MinAnimate /t REG_SZ /d 0 /f"
                    }
                },
                ["disable_snap_windows"] = new()
                {
                    Key = "disable_snap_windows",
                    Title = "Disable Snap Windows",
                    Category = "System",
                    Description = "Turns off Snap Assist and related multitasking helpers.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Control Panel\\Desktop\" /v WindowArrangementActive /t REG_SZ /d 0 /f",
                        "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v SnapAssist /t REG_DWORD /d 0 /f",
                        "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v JointResize /t REG_DWORD /d 0 /f",
                        "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v EnableSnapAssistFlyout /t REG_DWORD /d 0 /f"
                    }
                },
                ["disable_xbox_gamebar"] = new()
                {
                    Key = "disable_xbox_gamebar",
                    Title = "Disable Xbox Game Bar UI",
                    Category = "Gaming",
                    Description = "Turns off Game Bar startup/capture UI hooks.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v ShowStartupPanel /t REG_DWORD /d 0 /f",
                        "reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v UseNexusForGameBarEnabled /t REG_DWORD /d 0 /f",
                        "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR\" /v HistoricalCaptureEnabled /t REG_DWORD /d 0 /f"
                    }
                },
                ["disable_fullscreen_optimizations"] = new()
                {
                    Key = "disable_fullscreen_optimizations",
                    Title = "Disable Fullscreen Optimizations",
                    Category = "Gaming",
                    Description = "Applies global fullscreen optimization compatibility flags.",
                    Risk = TweakRisk.Caution,
                    Warning = "Some older games may prefer the default fullscreen behavior.",
                    Commands = new[]
                    {
                        "reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_FSEBehaviorMode /t REG_DWORD /d 2 /f",
                        "reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_HonorUserFSEBehaviorMode /t REG_DWORD /d 1 /f",
                        "reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_DXGIHonorFSEWindowsCompatible /t REG_DWORD /d 1 /f"
                    }
                },
                ["low_latency_boot_profile"] = new()
                {
                    Key = "low_latency_boot_profile",
                    Title = "Low-Latency Boot Profile",
                    Category = "Gaming",
                    Description = "Applies aggressive BCD timer and virtualization boot flags.",
                    Risk = TweakRisk.Caution,
                    Warning = "Can affect Hyper-V, VBS, virtualization, and some anti-cheat setups.",
                    RequiresRestart = true,
                    Commands = new[]
                    {
                        "bcdedit /set disabledynamictick yes",
                        "bcdedit /set useplatformtick yes",
                        "bcdedit /set tscsyncpolicy Enhanced",
                        "bcdedit /set tpmbootentropy ForceDisable",
                        "bcdedit /set hypervisorlaunchtype off",
                        "bcdedit /set quietboot yes",
                        "bcdedit /set allowedinmemorysettings 0x0",
                        "bcdedit /set isolatedcontext No"
                    }
                },
                ["tcp_nonsack_rtt_resiliency_disabled"] = new()
                {
                    Key = "tcp_nonsack_rtt_resiliency_disabled",
                    Title = "Disable TCP Non-SACK RTT Resiliency",
                    Category = "Network",
                    Description = "Disables non-SACK RTT resiliency behavior.",
                    Risk = TweakRisk.Caution,
                    Warning = "Can be less stable on highly lossy routes.",
                    Commands = new[]
                    {
                        "netsh int tcp set global nonsackrttresiliency=disabled"
                    }
                },
                ["tcp_initial_rto_2000"] = new()
                {
                    Key = "tcp_initial_rto_2000",
                    Title = "TCP Initial RTO 2000ms",
                    Category = "Network",
                    Description = "Sets initial retransmission timeout to 2000 ms.",
                    Risk = TweakRisk.Caution,
                    Warning = "Network behavior can vary between routers/ISPs.",
                    Commands = new[]
                    {
                        "netsh int tcp set global initialrto=2000"
                    }
                },
                ["disable_pca_service"] = new()
                {
                    Key = "disable_pca_service",
                    Title = "Disable Program Compatibility Assistant",
                    Category = "Services",
                    Description = "Disables Program Compatibility Assistant service.",
                    Risk = TweakRisk.Caution,
                    Warning = "Compatibility warnings/prompts may no longer appear.",
                    Commands = new[]
                    {
                        "sc query PcaSvc >nul 2>&1 && (sc stop PcaSvc & sc config PcaSvc start= disabled) || exit /b 0"
                    }
                },
                ["disable_fax_service"] = new()
                {
                    Key = "disable_fax_service",
                    Title = "Disable Fax Service",
                    Category = "Services",
                    Description = "Disables legacy Windows Fax service.",
                    Risk = TweakRisk.Caution,
                    Warning = "Built-in fax features will be unavailable.",
                    Commands = new[]
                    {
                        "sc query Fax >nul 2>&1 && (sc stop Fax & sc config Fax start= disabled) || exit /b 0"
                    }
                },
                ["clear_directx_shader_cache"] = new()
                {
                    Key = "clear_directx_shader_cache",
                    Title = "DirectX Shader Cache Cleanup",
                    Category = "Maintenance",
                    Description = "Cleans DirectX shader cache in local profile.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "if exist \"%LocalAppData%\\D3DSCache\" del /f /s /q \"%LocalAppData%\\D3DSCache\\*\""
                    }
                },
                ["disable_widgets"] = new()
                {
                    Key = "disable_widgets",
                    Title = "Disable Widgets",
                    Category = "System",
                    Description = "Turns off widgets/taskbar feed components.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v TaskbarDa /t REG_DWORD /d 0 /f",
                        "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Dsh\" /v AllowNewsAndInterests /t REG_DWORD /d 0 /f"
                    }
                },
                ["disable_copilot"] = new()
                {
                    Key = "disable_copilot",
                    Title = "Disable Copilot",
                    Category = "System",
                    Description = "Disables Windows Copilot entry points.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Software\\Policies\\Microsoft\\Windows\\WindowsCopilot\" /v TurnOffWindowsCopilot /t REG_DWORD /d 1 /f"
                    }
                },
                ["disable_edge_background_mode"] = new()
                {
                    Key = "disable_edge_background_mode",
                    Title = "Disable Edge Background Mode",
                    Category = "Services",
                    Description = "Prevents Microsoft Edge from running in background.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /v BackgroundModeEnabled /t REG_DWORD /d 0 /f"
                    }
                },
                ["disable_onedrive_startup"] = new()
                {
                    Key = "disable_onedrive_startup",
                    Title = "Disable OneDrive Startup",
                    Category = "Services",
                    Description = "Disables OneDrive auto-start entry for current user.",
                    Risk = TweakRisk.Caution,
                    Warning = "OneDrive will not auto-start with Windows.",
                    Commands = new[]
                    {
                        "reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run\" /v OneDrive /f >nul 2>&1 || exit /b 0"
                    }
                },
                ["disable_teams_autostart"] = new()
                {
                    Key = "disable_teams_autostart",
                    Title = "Disable Teams Auto Start",
                    Category = "Services",
                    Description = "Disables common Teams startup entries.",
                    Risk = TweakRisk.Caution,
                    Warning = "Teams will not auto-start after sign-in.",
                    Commands = new[]
                    {
                        "reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run\" /v com.squirrel.Teams.Teams /f >nul 2>&1 || exit /b 0",
                        "reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run\" /v msteams /f >nul 2>&1 || exit /b 0"
                    }
                },
                ["disable_phone_service"] = new()
                {
                    Key = "disable_phone_service",
                    Title = "Disable Phone Service",
                    Category = "Services",
                    Description = "Disables Phone Service background process.",
                    Risk = TweakRisk.Caution,
                    Warning = "Phone Link related integrations may stop working.",
                    Commands = new[]
                    {
                        "sc query PhoneSvc >nul 2>&1 && (sc stop PhoneSvc & sc config PhoneSvc start= disabled) || exit /b 0"
                    }
                },
                ["disable_start_recommendations"] = new()
                {
                    Key = "disable_start_recommendations",
                    Title = "Disable Start Recommendations",
                    Category = "System",
                    Description = "Disables Start menu recommendation entries.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v Start_IrisRecommendations /t REG_DWORD /d 0 /f"
                    }
                },
                ["disable_windows_spotlight"] = new()
                {
                    Key = "disable_windows_spotlight",
                    Title = "Disable Windows Spotlight",
                    Category = "System",
                    Description = "Disables Spotlight and lock screen suggestion content.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v RotatingLockScreenEnabled /t REG_DWORD /d 0 /f",
                        "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v RotatingLockScreenOverlayEnabled /t REG_DWORD /d 0 /f",
                        "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v SubscribedContent-338387Enabled /t REG_DWORD /d 0 /f"
                    }
                },
                ["disable_advertising_id"] = new()
                {
                    Key = "disable_advertising_id",
                    Title = "Disable Advertising ID",
                    Category = "System",
                    Description = "Turns off per-user advertising identifier tracking.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo\" /v Enabled /t REG_DWORD /d 0 /f"
                    }
                },
                ["disable_language_list_access"] = new()
                {
                    Key = "disable_language_list_access",
                    Title = "Disable Language List Access",
                    Category = "System",
                    Description = "Prevents websites from using your language list for local content.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Control Panel\\International\\User Profile\" /v HttpAcceptLanguageOptOut /t REG_DWORD /d 1 /f"
                    }
                },
                ["disable_app_launch_tracking"] = new()
                {
                    Key = "disable_app_launch_tracking",
                    Title = "Disable App Launch Tracking",
                    Category = "System",
                    Description = "Stops Windows from tracking app launches for Start and search.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v Start_TrackProgs /t REG_DWORD /d 0 /f"
                    }
                },
                ["disable_tailored_experiences"] = new()
                {
                    Key = "disable_tailored_experiences",
                    Title = "Disable Tailored Experiences",
                    Category = "System",
                    Description = "Turns off tailored suggestions that use diagnostic data.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy\" /v TailoredExperiencesWithDiagnosticDataEnabled /t REG_DWORD /d 0 /f"
                    }
                },
                ["disable_consumer_features"] = new()
                {
                    Key = "disable_consumer_features",
                    Title = "Disable Consumer Features",
                    Category = "Services",
                    Description = "Disables Windows consumer content and app suggestions.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\" /v DisableWindowsConsumerFeatures /t REG_DWORD /d 1 /f"
                    }
                },
                ["disable_remote_registry"] = new()
                {
                    Key = "disable_remote_registry",
                    Title = "Disable Remote Registry",
                    Category = "Services",
                    Description = "Disables Remote Registry service if not needed.",
                    Risk = TweakRisk.Caution,
                    Warning = "Remote registry management tools will stop working.",
                    Commands = new[]
                    {
                        "sc query RemoteRegistry >nul 2>&1 && (sc stop RemoteRegistry & sc config RemoteRegistry start= disabled) || exit /b 0"
                    }
                },
                ["disable_ssdp_upnp_services"] = new()
                {
                    Key = "disable_ssdp_upnp_services",
                    Title = "Disable SSDP & UPnP Services",
                    Category = "Services",
                    Description = "Disables SSDP discovery and UPnP host services.",
                    Risk = TweakRisk.Caution,
                    Warning = "Network discovery and auto port mapping may stop working.",
                    Commands = new[]
                    {
                        "sc query SSDPSRV >nul 2>&1 && (sc stop SSDPSRV & sc config SSDPSRV start= disabled) || exit /b 0",
                        "sc query upnphost >nul 2>&1 && (sc stop upnphost & sc config upnphost start= disabled) || exit /b 0"
                    }
                },
                ["disable_auto_maintenance"] = new()
                {
                    Key = "disable_auto_maintenance",
                    Title = "Disable Automatic Maintenance",
                    Category = "Services",
                    Description = "Disables scheduled automatic maintenance tasks.",
                    Risk = TweakRisk.Caution,
                    Warning = "Windows background maintenance and diagnostics may run less often.",
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Schedule\\Maintenance\" /v MaintenanceDisabled /t REG_DWORD /d 1 /f"
                    }
                },
                ["disable_windows_update_access"] = new()
                {
                    Key = "disable_windows_update_access",
                    Title = "Disable Windows Update Access",
                    Category = "Services",
                    Description = "Disables Windows Update policy access and auto update.",
                    Risk = TweakRisk.Caution,
                    Warning = "Security and driver updates will stop until the policy is reverted.",
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /v DoNotConnectToWindowsUpdateInternetLocations /t REG_DWORD /d 1 /f",
                        "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /v SetDisableUXWUAccess /t REG_DWORD /d 1 /f",
                        "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoUpdate /t REG_DWORD /d 1 /f",
                        "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\" /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 1 /f",
                        "sc stop wuauserv >nul 2>&1 || exit /b 0",
                        "sc stop UsoSvc >nul 2>&1 || exit /b 0"
                    }
                },
                ["disable_nagle_algorithm"] = new()
                {
                    Key = "disable_nagle_algorithm",
                    Title = "Disable Nagle Algorithm",
                    Category = "Network",
                    Description = "Disables delayed ACK/Nagle behavior on active interfaces.",
                    Risk = TweakRisk.Caution,
                    Warning = "Can behave differently depending on driver/router stack.",
                    Commands = new[]
                    {
                        "for /f %i in ('reg query \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\" /s /f \"DhcpIPAddress\" ^| findstr /i \"HKEY\"') do @reg add \"%i\" /v TcpAckFrequency /t REG_DWORD /d 1 /f",
                        "for /f %i in ('reg query \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\" /s /f \"DhcpIPAddress\" ^| findstr /i \"HKEY\"') do @reg add \"%i\" /v TcpNoDelay /t REG_DWORD /d 1 /f",
                        "for /f %i in ('reg query \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\" /s /f \"DhcpIPAddress\" ^| findstr /i \"HKEY\"') do @reg add \"%i\" /v TcpDelAckTicks /t REG_DWORD /d 0 /f"
                    }
                },
                ["disable_task_offload"] = new()
                {
                    Key = "disable_task_offload",
                    Title = "Disable Task Offload",
                    Category = "Network",
                    Description = "Disables legacy network task offload path in TCP/IP stack.",
                    Risk = TweakRisk.Caution,
                    Warning = "Some older NIC drivers may behave differently after the change.",
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\" /v DisableTaskOffload /t REG_DWORD /d 1 /f"
                    }
                },
                ["disable_qos_reserved_bandwidth"] = new()
                {
                    Key = "disable_qos_reserved_bandwidth",
                    Title = "Disable QoS Reserved Bandwidth",
                    Category = "Network",
                    Description = "Sets QoS reserved bandwidth limit to 0%.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Psched\" /v NonBestEffortLimit /t REG_DWORD /d 0 /f"
                    }
                },
                ["disable_memory_compression"] = new()
                {
                    Key = "disable_memory_compression",
                    Title = "Disable Memory Compression",
                    Category = "System",
                    Description = "Disables memory compression and page combining.",
                    Risk = TweakRisk.Caution,
                    Warning = "May reduce performance on low-RAM systems.",
                    Commands = new[]
                    {
                        "powershell -NoProfile -Command \"Disable-MMAgent -MemoryCompression\" >nul 2>&1 || exit /b 0",
                        "reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v DisablePageCombining /t REG_DWORD /d 1 /f"
                    }
                },
                ["ntfs_disable_8dot3"] = new()
                {
                    Key = "ntfs_disable_8dot3",
                    Title = "Disable NTFS 8dot3 Names",
                    Category = "System",
                    Description = "Disables legacy 8.3 filename generation on NTFS.",
                    Risk = TweakRisk.Caution,
                    Warning = "Very old installers/scripts may rely on 8.3 paths.",
                    Commands = new[]
                    {
                        "fsutil behavior set disable8dot3 1",
                        "reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem\" /v NtfsDisable8dot3NameCreation /t REG_DWORD /d 1 /f"
                    }
                },
                ["disable_storage_sense"] = new()
                {
                    Key = "disable_storage_sense",
                    Title = "Disable Storage Sense",
                    Category = "Services",
                    Description = "Disables Storage Sense background cleanup automation.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy\" /v 01 /t REG_DWORD /d 0 /f"
                    }
                },
                ["clean_windows_update_cache"] = new()
                {
                    Key = "clean_windows_update_cache",
                    Title = "Clean Windows Update Cache",
                    Category = "Maintenance",
                    Description = "Cleans Windows Update download cache.",
                    Risk = TweakRisk.Caution,
                    Warning = "Can reset in-progress update downloads.",
                    Commands = new[]
                    {
                        "net stop wuauserv >nul 2>&1 || exit /b 0",
                        "if exist \"%WINDIR%\\SoftwareDistribution\\Download\" del /f /s /q \"%WINDIR%\\SoftwareDistribution\\Download\\*\"",
                        "net start wuauserv >nul 2>&1 || exit /b 0"
                    }
                },
                ["reset_windows_update_folder"] = new()
                {
                    Key = "reset_windows_update_folder",
                    Title = "Reset Windows Update Folder",
                    Category = "Maintenance",
                    Description = "Rebuilds the SoftwareDistribution folder from scratch.",
                    Risk = TweakRisk.Caution,
                    Warning = "Resets update history cache and in-progress update files.",
                    Commands = new[]
                    {
                        "net stop wuauserv >nul 2>&1 || exit /b 0",
                        "net stop UsoSvc >nul 2>&1 || exit /b 0",
                        "rd /s /q \"%WINDIR%\\SoftwareDistribution\" >nul 2>&1 || exit /b 0",
                        "md \"%WINDIR%\\SoftwareDistribution\" >nul 2>&1 || exit /b 0",
                        "net start wuauserv >nul 2>&1 || exit /b 0",
                        "net start UsoSvc >nul 2>&1 || exit /b 0"
                    }
                },
                ["clean_thumbnail_cache"] = new()
                {
                    Key = "clean_thumbnail_cache",
                    Title = "Clean Thumbnail Cache",
                    Category = "Maintenance",
                    Description = "Cleans Windows Explorer thumbnail cache database files.",
                    Recommended = true,
                    Commands = new[]
                    {
                        "del /f /q \"%LocalAppData%\\Microsoft\\Windows\\Explorer\\thumbcache*.db\" >nul 2>&1 || exit /b 0"
                    }
                }
            };

        public static SystemTweakDefinition? Get(string key)
            => All.TryGetValue(key, out var value) ? value : null;
    }
}

