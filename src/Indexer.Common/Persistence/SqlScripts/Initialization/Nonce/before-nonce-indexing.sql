-- Requirements: PostgreSQL v11

-- General

set search_path to @schemaName;

-- Nonces

create unlogged table nonces
(
    address             varchar(256) not null,
    transaction_id      varchar(256) not null,
    value               bigint not null,    

    constraint pk_nonces primary key (address, transaction_id)
) partition by hash (address, transaction_id);

create table nonces_0 partition of nonces for values with (modulus 20, remainder 0);
create table nonces_1 partition of nonces for values with (modulus 20, remainder 1);
create table nonces_2 partition of nonces for values with (modulus 20, remainder 2);
create table nonces_3 partition of nonces for values with (modulus 20, remainder 3);
create table nonces_4 partition of nonces for values with (modulus 20, remainder 4);
create table nonces_5 partition of nonces for values with (modulus 20, remainder 5);
create table nonces_6 partition of nonces for values with (modulus 20, remainder 6);
create table nonces_7 partition of nonces for values with (modulus 20, remainder 7);
create table nonces_8 partition of nonces for values with (modulus 20, remainder 8);
create table nonces_9 partition of nonces for values with (modulus 20, remainder 9);
create table nonces_10 partition of nonces for values with (modulus 20, remainder 10);
create table nonces_11 partition of nonces for values with (modulus 20, remainder 11);
create table nonces_12 partition of nonces for values with (modulus 20, remainder 12);
create table nonces_13 partition of nonces for values with (modulus 20, remainder 13);
create table nonces_14 partition of nonces for values with (modulus 20, remainder 14);
create table nonces_15 partition of nonces for values with (modulus 20, remainder 15);
create table nonces_16 partition of nonces for values with (modulus 20, remainder 16);
create table nonces_17 partition of nonces for values with (modulus 20, remainder 17);
create table nonces_18 partition of nonces for values with (modulus 20, remainder 18);
create table nonces_19 partition of nonces for values with (modulus 20, remainder 19);