-- General

create schema @schemaName;

set search_path to @schemaName;

-- Block headers

create table block_headers
(
    id             varchar(256) not null,
    number         bigint not null,
    previous_id    varchar(256) null,
    mined_at        timestamp not null,

    constraint pk_block_headers primary key (id)
);

create unique index ix_bock_headers_number
    on block_headers (number);
   
alter table block_headers set unlogged;

-- Transaction headers

create table transaction_headers
(
    id               varchar(256) not null,
    block_id         varchar(256) not null,
    number           int not null,
    error_message    text,
    error_code       int,

    constraint pk_transaction_headers primary key (id)
);

alter table transaction_headers set unlogged;