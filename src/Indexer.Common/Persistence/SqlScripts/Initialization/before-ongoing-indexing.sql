﻿set search_path to @schemaName;

-- Turns 'logged' on

alter table block_headers set logged;
alter table transaction_headers set logged;
alter table balance_updates set logged;
alter table fees set logged;

-- Creates indexes

create index ix_balance_updates_block_id
    on balance_updates (block_id);