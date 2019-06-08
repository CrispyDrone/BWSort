CREATE TEMP TABLE ReplaysToDelete
as
	select PR.replayid, PR.backupid
	from 
		(
			select replayid, backupid, count(*) over (partition by replayid) as NumberOfBackups
			from replaybackups
		) PR
	where PR.backupid = @BackupId and PR.NumberOfBackups = 1;

delete
from replays 
where id in 
	(
		select id
		from replays r
		inner join ReplaysToDelete rtd
		on r.id = rtd.replayId
	);

delete
from replaybackups
where replayid in 
	(
		select rb.replayid
		from replaybackups rb
		inner join ReplaysToDelete rtd
		on rb.replayid = rtd.replayid and rb.backupid = rtd.backupid
	)
	and backupid = @BackupId;

delete from backups
where id=@BackupId;
