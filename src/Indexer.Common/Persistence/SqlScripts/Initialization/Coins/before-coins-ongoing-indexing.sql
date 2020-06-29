set search_path to @schemaName;

-- Turns 'logged' on

alter table input_coins set logged;
alter table unspent_coins set logged;
alter table spent_coins set logged;

-- Creates indexes

create index ix_unspent_coins_address
    on unspent_coins (address);