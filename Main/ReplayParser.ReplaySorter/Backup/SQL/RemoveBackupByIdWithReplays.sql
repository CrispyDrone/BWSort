With ReplaysToDelete
as
(
	select replayId, @BackupId as backupid, count(*) as NumberOfBackups
	from replaybackups
	where backupid = @BackupId
	group by replayId
	having count(*) = 1
)
delete r 
from replays r
inner join ReplaysToDelete rtd
on r.id = rtd.replayId;

delete rb
from replaybackups rb
inner join ReplaysToDelete rtd
on rb.replayid = rtd.replayid and rb.backupid = rtd.backupid;

delete from backups
where backupid=@BackupId;
