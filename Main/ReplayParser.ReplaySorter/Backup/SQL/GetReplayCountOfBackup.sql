select count(*)
from backups b
inner join replaybackups rb
on b.id = rb.backupid
where b.id = @Id
group by b.id;
