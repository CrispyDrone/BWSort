insert into replays (hash, bytes)
values (@Hash, @Bytes);
select last_insert_rowid();
