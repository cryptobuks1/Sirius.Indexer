﻿-- Requirements: PostgreSQL v11

-- General

create schema @schemaName;

set search_path to @schemaName;

-- Block headers

create unlogged table block_headers
(
    id             varchar(256) not null,
    number         bigint not null,
    previous_id    varchar(256) null,
    mined_at       timestamp not null,

    constraint pk_block_headers primary key (id)
);

create unique index ix_block_headers_number
    on block_headers (number);
  
-- Transaction headers

create unlogged table transaction_headers
(
    id                  varchar(256) not null,
    block_id            varchar(256) not null,
    number              int not null,
    error_message       text,
    error_code          int,

    constraint pk_transaction_headers primary key (id)
)  partition by hash (id);

-- TODO: Is it needed for nonce blockchains?

create index ix_transaction_headers_block_id
    on transaction_headers (block_id);

create table transaction_headers_0 partition of transaction_headers for values with (modulus 10, remainder 0);
create table transaction_headers_1 partition of transaction_headers for values with (modulus 10, remainder 1);
create table transaction_headers_2 partition of transaction_headers for values with (modulus 10, remainder 2);
create table transaction_headers_3 partition of transaction_headers for values with (modulus 10, remainder 3);
create table transaction_headers_4 partition of transaction_headers for values with (modulus 10, remainder 4);
create table transaction_headers_5 partition of transaction_headers for values with (modulus 10, remainder 5);
create table transaction_headers_6 partition of transaction_headers for values with (modulus 10, remainder 6);
create table transaction_headers_7 partition of transaction_headers for values with (modulus 10, remainder 7);
create table transaction_headers_8 partition of transaction_headers for values with (modulus 10, remainder 8);
create table transaction_headers_9 partition of transaction_headers for values with (modulus 10, remainder 9);

-- Balance updates

create unlogged table balance_updates
(
    address         varchar(256) not null,
    asset_id        bigint not null,
    block_number    bigint not null,
    block_id        varchar(256) not null,
    block_mined_at  timestamp not null,
    amount          numeric not null,

    constraint pk_balance_updates primary key (address, asset_id, block_number)
) partition by hash (address, asset_id);

create table balance_updates_0 partition of balance_updates for values with (modulus 20, remainder 0);
create table balance_updates_1 partition of balance_updates for values with (modulus 20, remainder 1);
create table balance_updates_2 partition of balance_updates for values with (modulus 20, remainder 2);
create table balance_updates_3 partition of balance_updates for values with (modulus 20, remainder 3);
create table balance_updates_4 partition of balance_updates for values with (modulus 20, remainder 4);
create table balance_updates_5 partition of balance_updates for values with (modulus 20, remainder 5);
create table balance_updates_6 partition of balance_updates for values with (modulus 20, remainder 6);
create table balance_updates_7 partition of balance_updates for values with (modulus 20, remainder 7);
create table balance_updates_8 partition of balance_updates for values with (modulus 20, remainder 8);
create table balance_updates_9 partition of balance_updates for values with (modulus 20, remainder 9);
create table balance_updates_10 partition of balance_updates for values with (modulus 20, remainder 10);
create table balance_updates_11 partition of balance_updates for values with (modulus 20, remainder 11);
create table balance_updates_12 partition of balance_updates for values with (modulus 20, remainder 12);
create table balance_updates_13 partition of balance_updates for values with (modulus 20, remainder 13);
create table balance_updates_14 partition of balance_updates for values with (modulus 20, remainder 14);
create table balance_updates_15 partition of balance_updates for values with (modulus 20, remainder 15);
create table balance_updates_16 partition of balance_updates for values with (modulus 20, remainder 16);
create table balance_updates_17 partition of balance_updates for values with (modulus 20, remainder 17);
create table balance_updates_18 partition of balance_updates for values with (modulus 20, remainder 18);
create table balance_updates_19 partition of balance_updates for values with (modulus 20, remainder 19);

-- Fees

create unlogged table fees
(
    transaction_id      varchar(256) not null,
    asset_id            bigint not null,
    amount              numeric not null,

    constraint pk_fees primary key (transaction_id, asset_id)
) partition by hash (transaction_id, asset_id);

create table fees_0 partition of fees for values with (modulus 20, remainder 0);
create table fees_1 partition of fees for values with (modulus 20, remainder 1);
create table fees_2 partition of fees for values with (modulus 20, remainder 2);
create table fees_3 partition of fees for values with (modulus 20, remainder 3);
create table fees_4 partition of fees for values with (modulus 20, remainder 4);
create table fees_5 partition of fees for values with (modulus 20, remainder 5);
create table fees_6 partition of fees for values with (modulus 20, remainder 6);
create table fees_7 partition of fees for values with (modulus 20, remainder 7);
create table fees_8 partition of fees for values with (modulus 20, remainder 8);
create table fees_9 partition of fees for values with (modulus 20, remainder 9);
create table fees_10 partition of fees for values with (modulus 20, remainder 10);
create table fees_11 partition of fees for values with (modulus 20, remainder 11);
create table fees_12 partition of fees for values with (modulus 20, remainder 12);
create table fees_13 partition of fees for values with (modulus 20, remainder 13);
create table fees_14 partition of fees for values with (modulus 20, remainder 14);
create table fees_15 partition of fees for values with (modulus 20, remainder 15);
create table fees_16 partition of fees for values with (modulus 20, remainder 16);
create table fees_17 partition of fees for values with (modulus 20, remainder 17);
create table fees_18 partition of fees for values with (modulus 20, remainder 18);
create table fees_19 partition of fees for values with (modulus 20, remainder 19);

-- Observed operations

create table observed_operations
(
    id                  bigint not null,
    transaction_id      varchar(256) not null,
    added_at            timestamp not null,

    constraint pk_observed_operations primary key (id)
);

create unique index ix_observed_operations_transaction_id
    on observed_operations (transaction_id);