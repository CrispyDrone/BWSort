select Id, Name, Comment, RootDirectory, Date
from backups
where id=@Id;

select r.Id, r.Hash, r.Bytes, rb.FileName
from backups b
inner join replaybackups rb on b.id = rb.backupid
inner join replays r on rb.replayid = r.id
where b.id=@Id;