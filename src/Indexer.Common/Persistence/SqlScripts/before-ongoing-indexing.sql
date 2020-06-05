set search_path to @schemaName;

-- Turns 'logged' on

alter table block_headers set logged;
alter table transaction_headers set logged;

-- Creates indexes

create index if not exists ix_transaction_headers_block_id
    on transaction_headers (block_id);