insert into backups (name, comment, rootdirectory, date)
values (@Name, @Comment, @RootDirectory,date('now'));
select last_insert_rowid();
