using Ducky.Sdk.Attributes;

namespace Ducky.ModArk;

[LanguageSupport("en", "zh-Hant", "zh", "fr", "de", "es", "ru", "ja", "ko", "pt")]
public static class LK
{
    public static class Terminal
    {
        public const string TerminalDescription = "ma_terminal_desc";

        public const string BackupCommandDescription = "ma_terminal_backup_cmd_desc";
        public const string BackupCommandNameArgumentDescription = "ma_terminal_backup_cmd_name_arg_desc";
        public const string BackupNameRequiredError = "ma_terminal_backup_name_required_error";
        public const string BackupCompletedLog = "ma_terminal_backup_completed_log";
        public const string BackupFailedLog = "ma_terminal_backup_failed_log";

        public const string RestoreCommandDescription = "ma_terminal_restore_cmd_desc";
        public const string RestoreYesOptionDescription = "ma_terminal_restore_yes_opt_desc";
        public const string RestoreFailedLog = "ma_terminal_restore_failed_log";
        public const string RestoreTimeoutError = "ma_terminal_restore_timeout_error";
        public const string RestoreReadFileError = "ma_terminal_restore_read_file_error";
        public const string RestoreInvalidFileError = "ma_terminal_restore_invalid_file_error";
        public const string RestoreMissingFieldsError = "ma_terminal_restore_missing_fields_error";
        public const string RestoreNeedYesError = "ma_terminal_restore_need_yes_error";
        public const string RestoreSteamNotInitializedError = "ma_terminal_restore_steam_not_initialized_error";
        public const string RestoreCompletedLog = "ma_terminal_restore_completed_log";
        public const string RestoreFolderCountdownLog = "ma_terminal_restore_folder_countdown_log";
        public const string RestoreShutdownWarningLog = "ma_terminal_restore_shutdown_warning_log";
        public const string RestoreSubscribeSuccessLog = "ma_terminal_restore_subscribe_success_log";
        public const string RestoreUnsubscribeSuccessLog = "ma_terminal_restore_unsubscribe_success_log";
    }
}


