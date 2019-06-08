delete 
from replaybackups
where backupid = @BackupId;

delete 
from backups
where id = @BackupId;