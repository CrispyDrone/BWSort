CREATE TEMP TABLE IF NOT EXISTS Variables (Name TEXT PRIMARY KEY, Value TEXT);

INSERT INTO Variables (Name, Value) 
VALUES	('NumberOfTables', 
			CAST((
				select 
					case count(*)
						when 0 then 0
						when 3 then 1
						else 2
					end
				from sqlite_master
				where type='table' and tbl_name not like '%sequence%'
			) as text)
		);

INSERT INTO Variables (Name, Value)
VALUES	('TableNamesOk',
			CAST((
				select
					case count(*)
						when 0 then 0
						when 3 then 1
						else 2
					end
				from sqlite_master
				where type='table' and tbl_name in ('replays', 'backups', 'replaybackups')
			) as text)
		);

select CAST(CASE GROUP_CONCAT(Value, '')
			when '00' then 0
			when '11' then 1
			else 2
		end as integer)
from Variables;

DROP TABLE Variables;
	
/*
	  NumberOfTables	TableNamesOk		=> 9 possible combinations
			0				0				=> empty	
			1				0				=> invalid
			2				0				=> invalid
			0				1				=> impossible
			1				1				=> valid
			2				1				=> invalid
			0				2				=> impossible
			1				2				=> invalid
			2				2				=> invalid
*/
