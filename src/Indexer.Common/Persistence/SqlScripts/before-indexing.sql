-- Requirements: PostgreSQL v11

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
