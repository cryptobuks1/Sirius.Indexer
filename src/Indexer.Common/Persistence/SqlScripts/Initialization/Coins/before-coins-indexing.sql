-- Requirements: PostgreSQL v11

-- General

set search_path to @schemaName;

-- Input coins

create unlogged table input_coins
(
    transaction_id                  varchar(256) not null,
    number                          int not null,
    type                            int not null,
    prev_output_transaction_id      varchar(256),
    prev_output_coin_number         int,    

    constraint pk_input_coins primary key (transaction_id, number)
) partition by hash (transaction_id, number);

create table input_coins_0 partition of input_coins for values with (modulus 20, remainder 0);
create table input_coins_1 partition of input_coins for values with (modulus 20, remainder 1);
create table input_coins_2 partition of input_coins for values with (modulus 20, remainder 2);
create table input_coins_3 partition of input_coins for values with (modulus 20, remainder 3);
create table input_coins_4 partition of input_coins for values with (modulus 20, remainder 4);
create table input_coins_5 partition of input_coins for values with (modulus 20, remainder 5);
create table input_coins_6 partition of input_coins for values with (modulus 20, remainder 6);
create table input_coins_7 partition of input_coins for values with (modulus 20, remainder 7);
create table input_coins_8 partition of input_coins for values with (modulus 20, remainder 8);
create table input_coins_9 partition of input_coins for values with (modulus 20, remainder 9);
create table input_coins_10 partition of input_coins for values with (modulus 20, remainder 10);
create table input_coins_11 partition of input_coins for values with (modulus 20, remainder 11);
create table input_coins_12 partition of input_coins for values with (modulus 20, remainder 12);
create table input_coins_13 partition of input_coins for values with (modulus 20, remainder 13);
create table input_coins_14 partition of input_coins for values with (modulus 20, remainder 14);
create table input_coins_15 partition of input_coins for values with (modulus 20, remainder 15);
create table input_coins_16 partition of input_coins for values with (modulus 20, remainder 16);
create table input_coins_17 partition of input_coins for values with (modulus 20, remainder 17);
create table input_coins_18 partition of input_coins for values with (modulus 20, remainder 18);
create table input_coins_19 partition of input_coins for values with (modulus 20, remainder 19);

-- Unspent coins

create unlogged table unspent_coins
(
    transaction_id      varchar(256) not null,
    number              int not null,
    asset_id            bigint not null,
    amount              numeric not null,
    address             varchar(256),
    script_pub_key      varchar(1024),
    tag                 varchar(1024),
    tag_type            int,

    constraint pk_unspent_coins primary key (transaction_id, number)
) partition by hash (transaction_id, number);

create table unspent_coins_0 partition of unspent_coins for values with (modulus 20, remainder 0);
create table unspent_coins_1 partition of unspent_coins for values with (modulus 20, remainder 1);
create table unspent_coins_2 partition of unspent_coins for values with (modulus 20, remainder 2);
create table unspent_coins_3 partition of unspent_coins for values with (modulus 20, remainder 3);
create table unspent_coins_4 partition of unspent_coins for values with (modulus 20, remainder 4);
create table unspent_coins_5 partition of unspent_coins for values with (modulus 20, remainder 5);
create table unspent_coins_6 partition of unspent_coins for values with (modulus 20, remainder 6);
create table unspent_coins_7 partition of unspent_coins for values with (modulus 20, remainder 7);
create table unspent_coins_8 partition of unspent_coins for values with (modulus 20, remainder 8);
create table unspent_coins_9 partition of unspent_coins for values with (modulus 20, remainder 9);
create table unspent_coins_10 partition of unspent_coins for values with (modulus 20, remainder 10);
create table unspent_coins_11 partition of unspent_coins for values with (modulus 20, remainder 11);
create table unspent_coins_12 partition of unspent_coins for values with (modulus 20, remainder 12);
create table unspent_coins_13 partition of unspent_coins for values with (modulus 20, remainder 13);
create table unspent_coins_14 partition of unspent_coins for values with (modulus 20, remainder 14);
create table unspent_coins_15 partition of unspent_coins for values with (modulus 20, remainder 15);
create table unspent_coins_16 partition of unspent_coins for values with (modulus 20, remainder 16);
create table unspent_coins_17 partition of unspent_coins for values with (modulus 20, remainder 17);
create table unspent_coins_18 partition of unspent_coins for values with (modulus 20, remainder 18);
create table unspent_coins_19 partition of unspent_coins for values with (modulus 20, remainder 19);

-- Spent coins

create unlogged table spent_coins
(
    transaction_id              varchar(256) not null,
    number                      int not null,
    asset_id                    bigint not null,
    amount                      numeric not null,
    address                     varchar(256),
    script_pub_key              varchar(1024),
    tag                         varchar(1024),
    tag_type                    int,
    spent_by_transaction_id     varchar(256) not null,
    spent_by_input_coin_number  int not null,

    constraint pk_spent_coins primary key (transaction_id, number)
) partition by hash (transaction_id, number);

create table spent_coins_0 partition of spent_coins for values with (modulus 20, remainder 0);
create table spent_coins_1 partition of spent_coins for values with (modulus 20, remainder 1);
create table spent_coins_2 partition of spent_coins for values with (modulus 20, remainder 2);
create table spent_coins_3 partition of spent_coins for values with (modulus 20, remainder 3);
create table spent_coins_4 partition of spent_coins for values with (modulus 20, remainder 4);
create table spent_coins_5 partition of spent_coins for values with (modulus 20, remainder 5);
create table spent_coins_6 partition of spent_coins for values with (modulus 20, remainder 6);
create table spent_coins_7 partition of spent_coins for values with (modulus 20, remainder 7);
create table spent_coins_8 partition of spent_coins for values with (modulus 20, remainder 8);
create table spent_coins_9 partition of spent_coins for values with (modulus 20, remainder 9);
create table spent_coins_10 partition of spent_coins for values with (modulus 20, remainder 10);
create table spent_coins_11 partition of spent_coins for values with (modulus 20, remainder 11);
create table spent_coins_12 partition of spent_coins for values with (modulus 20, remainder 12);
create table spent_coins_13 partition of spent_coins for values with (modulus 20, remainder 13);
create table spent_coins_14 partition of spent_coins for values with (modulus 20, remainder 14);
create table spent_coins_15 partition of spent_coins for values with (modulus 20, remainder 15);
create table spent_coins_16 partition of spent_coins for values with (modulus 20, remainder 16);
create table spent_coins_17 partition of spent_coins for values with (modulus 20, remainder 17);
create table spent_coins_18 partition of spent_coins for values with (modulus 20, remainder 18);
create table spent_coins_19 partition of spent_coins for values with (modulus 20, remainder 19);