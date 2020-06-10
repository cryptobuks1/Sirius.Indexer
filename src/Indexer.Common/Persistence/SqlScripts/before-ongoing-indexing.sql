set search_path to @schemaName;

-- Turns 'logged' on

alter table assets set logged;
alter table block_headers set logged;
alter table transaction_headers set logged;
alter table input_coins set logged;
alter table unspent_coins set logged;
alter table spent_coins set logged;
alter table balance_updates set logged;
alter table fees set logged;

-- Creates indexes

create index if not exists ix_transaction_headers_block_id
    on transaction_headers (block_id);