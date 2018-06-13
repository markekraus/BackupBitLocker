using System.Diagnostics;
using System.Management.Automation;
using System.Threading;


namespace BackupBitLocker
{
    class Program
    {
        private const int TimeOutMilliseconds = 300000;
        static void Main(string[] args)
        {
            using (var powershell = PowerShell.Create())
            {
                var stopwatch = Stopwatch.StartNew();
                powershell.AddScript(@"
$EncryptedVolumes = Get-BitLockerVolume | 
    Where-Object {$_.VolumeStatus -match 'encrypted|EncryptionInProgress'}
foreach ($volume in $EncryptedVolumes) {
    $KeyProtector = $volume.KeyProtector | 
        Where-Object {$_.KeyProtectorType -match 'RecoveryPassword'}
    if($KeyProtector) {
        $backupToAADBitLockerKeyProtectorSplat = @{
            ErrorAction = 'SilentlyContinue'
            MountPoint = $volume.MountPoint
            KeyProtectorId = $KeyProtector.KeyProtectorId
        }
        $null = BackupToAAD-BitLockerKeyProtector @backupToAADBitLockerKeyProtectorSplat
        $backupBitLockerKeyProtectorSplat = @{
            ErrorAction = 'SilentlyContinue'
            MountPoint = $volume.MountPoint
            KeyProtectorId = $KeyProtector.KeyProtectorId
        }
        $null = Backup-BitLockerKeyProtector @backupBitLockerKeyProtectorSplat
    }
}
");
                var handler = powershell.BeginInvoke();
                while (!handler.IsCompleted)
                {
                    Thread.Sleep(200);
                    if(stopwatch.Elapsed.Milliseconds >= TimeOutMilliseconds)
                        break;
                }
                stopwatch.Stop();
                powershell.EndInvoke(handler);
            }
        }
    }
}
