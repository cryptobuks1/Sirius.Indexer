set search_path to @schemaName;

-- Turns 'logged' on

alter table input_coins set logged;
alter table unspent_coins set logged;
alter table spent_coins set logged;