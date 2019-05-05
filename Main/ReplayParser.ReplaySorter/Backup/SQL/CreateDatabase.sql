create table replays (
	id integer primary key autoincrement,
	hash text NOT NULL,
	bytes blob NOT NULL
	UNIQUE ( hash )
) WITHOUT ROWID;

create table backups (
	id integer primary key autoincrement,
	name text NOT NULL,
	comment text,
	rootdirectory text NOT NULL,
	date text NOT NULL
) WITHOUT ROWID;

create table replaybackups (
	backupid integer NOT NULL,
	replayid integer NOT NULL,
	filename text NOT NULL
	PRIMARY KEY ( backupid, replayid )
) WITHOUT ROWID;