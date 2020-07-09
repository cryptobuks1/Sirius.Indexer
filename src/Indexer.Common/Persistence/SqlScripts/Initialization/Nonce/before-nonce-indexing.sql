-- Requirements: PostgreSQL v11

-- General

set search_path to @schemaName;

-- Nonces

create unlogged table nonce_updates
(
    address             varchar(256) not null,
    transaction_id      varchar(256) not null,
    value               bigint not null,    

    constraint pk_nonce_updates primary key (address, transaction_id)
) partition by hash (address, transaction_id);

create table nonce_updates_0 partition of nonce_updates for values with (modulus 20, remainder 0);
create table nonce_updates_1 partition of nonce_updates for values with (modulus 20, remainder 1);
create table nonce_updates_2 partition of nonce_updates for values with (modulus 20, remainder 2);
create table nonce_updates_3 partition of nonce_updates for values with (modulus 20, remainder 3);
create table nonce_updates_4 partition of nonce_updates for values with (modulus 20, remainder 4);
create table nonce_updates_5 partition of nonce_updates for values with (modulus 20, remainder 5);
create table nonce_updates_6 partition of nonce_updates for values with (modulus 20, remainder 6);
create table nonce_updates_7 partition of nonce_updates for values with (modulus 20, remainder 7);
create table nonce_updates_8 partition of nonce_updates for values with (modulus 20, remainder 8);
create table nonce_updates_9 partition of nonce_updates for values with (modulus 20, remainder 9);
create table nonce_updates_10 partition of nonce_updates for values with (modulus 20, remainder 10);
create table nonce_updates_11 partition of nonce_updates for values with (modulus 20, remainder 11);
create table nonce_updates_12 partition of nonce_updates for values with (modulus 20, remainder 12);
create table nonce_updates_13 partition of nonce_updates for values with (modulus 20, remainder 13);
create table nonce_updates_14 partition of nonce_updates for values with (modulus 20, remainder 14);
create table nonce_updates_15 partition of nonce_updates for values with (modulus 20, remainder 15);
create table nonce_updates_16 partition of nonce_updates for values with (modulus 20, remainder 16);
create table nonce_updates_17 partition of nonce_updates for values with (modulus 20, remainder 17);
create table nonce_updates_18 partition of nonce_updates for values with (modulus 20, remainder 18);
create table nonce_updates_19 partition of nonce_updates for values with (modulus 20, remainder 19);