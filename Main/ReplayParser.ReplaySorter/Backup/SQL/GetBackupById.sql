select id, Name, Comment, RootDirectory, Date
from backups
where id=@Id;