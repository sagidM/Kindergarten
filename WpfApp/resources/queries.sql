--Full removing child
begin transaction

DECLARE @id int = 0   -- change this @id

delete from MonthlyPayments where ChildId = @id

delete from RangePayments where ChildId = @id

delete from EnterChildHistory where ChildId = @id

delete from Children where Id = @id

delete from People where Id = @id

commit
---------------------

