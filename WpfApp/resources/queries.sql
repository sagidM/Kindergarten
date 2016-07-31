--Full removing child
begin transaction

DECLARE @id int = 0


delete from EnterChildHistory where ChildId = @id

delete from Children where Id = @id

delete from People where Id = @id

commit
---------------------

