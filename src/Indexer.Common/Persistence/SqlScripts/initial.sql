/*
        private static void BuildTransactionHeaders(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TransactionHeaderEntity>(entityBuilder =>
            {
                entityBuilder.ToTable(TableNames.TransactionHeaders);
                entityBuilder.HasKey(x => x.GlobalId);
                
                entityBuilder.Property(x => x.BlockchainId).IsRequired();
                entityBuilder.Property(x => x.BlockId).IsRequired();
                entityBuilder.Property(x => x.Id).IsRequired();

                entityBuilder
                    .HasIndex(x => new
                    {
                        x.BlockchainId,
                        x.BlockId
                    })
                    .HasName("IX_TransactionHeaders_BlockchainId_BlockId");
            });
        }
        */

        /*
                private static void BuildBlockHeaders(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BlockHeaderEntity>(e =>
            {
                e.ToTable(TableNames.BlockHeaders);
                e.HasKey(x => x.GlobalId);

                e.HasIndex(x => new
                    {
                        x.BlockchainId,
                        x.Number
                    })
                    .IsUnique()
                    .HasName("IX_Blocks_BlockchainId_Number");

                e.Property(x => x.BlockchainId).IsRequired();
                e.Property(x => x.Id).IsRequired();
            });
        }

        */

        /*
        public string GlobalId { get; set; }
        public string BlockchainId { get; set; }
        public string Id { get; set; }
        public long Number { get; set; }
        public string PreviousId { get; set; }
        public DateTime MinedAt { get; set; }
        */


create table "%schema%".block_headers
(
    Id              varchar(256) not null,
    Number          bigint not null,
    PreviousId      varchar(256) null,
    MinedAt         timestamp not null

    constraint block_headers_pk primary key (Id)
)

create table coins
(
    id               uuid default uuid_generate_v1() not null,
    transaction_id   varchar(256)                            not null,
    coin_number      integer                         not null,
    asset_id         varchar(64)                            not null,
    coin_id          varchar(264)                            not null,
    asset_address    varchar(256),
    value            numeric                         not null,
    value_string     text                            not null,
    value_scale      integer                         not null,
    address          varchar(256),
    address_tag      varchar(1024),
    is_spent       boolean                         not null,
    address_tag_type smallint,
    address_nonce    numeric,
    block_number     bigint    not null,
    block_id         varchar(256)    not null
) partition by hash(address);

create table coins_1 partition of coins FOR VALUES WITH (MODULUS 10, REMAINDER 0);
alter table coins_1 add constraint coin1_pk primary key(id);
create unique index coins_1_natural_key_index
    on coins_1 (coin_id)  tablespace fast_space;
create index coins_1_block_id_index
	on coins_1 (block_id)  tablespace fast_space;
create index coins_1_address_coin_id_index
	on coins_1 (address, coin_id)  tablespace fast_space where is_spent =false;
create  trigger set_numeric_value_from_string_trigger before insert or update
     on coins_1 for each row
     execute procedure set_numeric_value_from_string ();

create table coins_2 partition of coins FOR VALUES WITH (MODULUS 10, REMAINDER 1);
alter table coins_2 add constraint coin2_pk primary key(id);
create unique index coins_2_natural_key_index
    on coins_2 (coin_id)  tablespace fast_space;
create index coins_2_block_id_index
	on coins_2 (block_id)  tablespace fast_space;
create index coins_2_address_coin_id_index
	on coins_2 (address, coin_id)  tablespace fast_space where is_spent =false;
create  trigger set_numeric_value_from_string_trigger before insert or update
     on coins_2 for each row
     execute procedure set_numeric_value_from_string ();

create table coins_3 partition of coins FOR VALUES WITH (MODULUS 10, REMAINDER 2);
alter table coins_3 add constraint coin3_pk primary key(id);
create unique index coins_3_natural_key_index
    on coins_3 (coin_id)  tablespace fast_space;
create index coins_3_block_id_index
	on coins_3 (block_id)  tablespace fast_space;
create index coins_3_address_coin_id_index
	on coins_3 (address, coin_id)  tablespace fast_space where is_spent =false;
create  trigger set_numeric_value_from_string_trigger before insert or update
     on coins_3 for each row
     execute procedure set_numeric_value_from_string ();

create table coins_4 partition of coins FOR VALUES WITH (MODULUS 10, REMAINDER 3);
alter table coins_4 add constraint coin4_pk primary key(id);
create unique index coins_4_natural_key_index
    on coins_4 (coin_id)  tablespace fast_space;
create index coins_4_block_id_index
	on coins_4 (block_id)  tablespace fast_space;
create index coins_4_address_coin_id_index
	on coins_4 (address, coin_id)  tablespace fast_space where is_spent =false;
create  trigger set_numeric_value_from_string_trigger before insert or update
     on coins_4 for each row
     execute procedure set_numeric_value_from_string ();

create table coins_5 partition of coins FOR VALUES WITH (MODULUS 10, REMAINDER 4);
alter table coins_5 add constraint coin5_pk primary key(id);
create unique index coins_5_natural_key_index
    on coins_5 (coin_id)  tablespace fast_space;
create index coins_5_block_id_index
	on coins_5 (block_id)  tablespace fast_space;
create index coins_5_address_coin_id_index
	on coins_5 (address, coin_id)  tablespace fast_space where is_spent =false;
create  trigger set_numeric_value_from_string_trigger before insert or update
     on coins_5 for each row
     execute procedure set_numeric_value_from_string ();

create table coins_6 partition of coins FOR VALUES WITH (MODULUS 10, REMAINDER 5);
alter table coins_6 add constraint coin6_pk primary key(id);
create unique index coins_6_natural_key_index
    on coins_6 (coin_id)  tablespace fast_space;
create index coins_6_block_id_index
	on coins_6 (block_id)  tablespace fast_space;
create index coins_6_address_coin_id_index
	on coins_6 (address, coin_id)  tablespace fast_space where is_spent =false;
create  trigger set_numeric_value_from_string_trigger before insert or update
     on coins_6 for each row
     execute procedure set_numeric_value_from_string ();

create table coins_7 partition of coins FOR VALUES WITH (MODULUS 10, REMAINDER 6);
alter table coins_7 add constraint coin7_pk primary key(id);
create unique index coins_7_natural_key_index
    on coins_7 (coin_id)  tablespace fast_space;
create index coins_7_block_id_index
	on coins_7 (block_id)  tablespace fast_space;
create index coins_7_address_coin_id_index
	on coins_7 (address, coin_id)  tablespace fast_space where is_spent =false;
create  trigger set_numeric_value_from_string_trigger before insert or update
     on coins_7 for each row
     execute procedure set_numeric_value_from_string ();

create table coins_8 partition of coins FOR VALUES WITH (MODULUS 10, REMAINDER 7);
alter table coins_8 add constraint coin8_pk primary key(id);
create unique index coins_8_natural_key_index
    on coins_8 (coin_id)  tablespace fast_space;
create index coins_8_block_id_index
	on coins_8 (block_id)  tablespace fast_space;
create index coins_8_address_coin_id_index
	on coins_8 (address, coin_id)  tablespace fast_space where is_spent =false;
create  trigger set_numeric_value_from_string_trigger before insert or update
     on coins_8 for each row
     execute procedure set_numeric_value_from_string ();

create table coins_9 partition of coins FOR VALUES WITH (MODULUS 10, REMAINDER 8);
alter table coins_9 add constraint coin9_pk primary key(id);
create unique index coins_9_natural_key_index
    on coins_9 (coin_id)  tablespace fast_space;
create index coins_9_block_id_index
	on coins_9 (block_id)  tablespace fast_space;
create index coins_9_address_coin_id_index
	on coins_9 (address, coin_id)  tablespace fast_space where is_spent =false;
create  trigger set_numeric_value_from_string_trigger before insert or update
     on coins_9 for each row
     execute procedure set_numeric_value_from_string ();

create table coins_10 partition of coins FOR VALUES WITH (MODULUS 10, REMAINDER 9);
alter table coins_10 add constraint coin10_pk primary key(id);
create unique index coins_10_natural_key_index
    on coins_10 (coin_id)  tablespace fast_space;
create index coins_10_block_id_index
	on coins_10 (block_id)  tablespace fast_space;
create index coins_10_address_coin_id_index
	on coins_10 (address, coin_id)  tablespace fast_space where is_spent =false;
create  trigger set_numeric_value_from_string_trigger before insert or update
     on coins_10 for each row
     execute procedure set_numeric_value_from_string ();

create table fees
(
    id              uuid default uuid_generate_v1() not null,
    block_id        varchar(256)                            not null,
    transaction_id  varchar(256)                            not null,
    asset_id        varchar(64)                            not null,
    asset_address   varchar(256),
    value           numeric                         not null,
    value_string    text                            not null,
    value_scale     integer                         not null,
    constraint fees_pk
        primary key (id)
);

create unique index fees_natural_key_index_1
    on fees (transaction_id, asset_id)  tablespace fast_space
where asset_address is null ;

create unique index fees_natural_key_index_2
    on fees (transaction_id, asset_id, asset_address)  tablespace fast_space
where asset_address is not null;

create index fees_block_id_index
	on fees (block_id)   tablespace fast_space;
    
create  trigger set_numeric_value_from_string_trigger before insert or update
     on fees for each row
     execute procedure set_numeric_value_from_string ();

create table balance_actions
(
    id              uuid default uuid_generate_v1(),
    block_id        varchar(256)    not null,
    block_number    integer not null,
    asset_id        varchar(64)    not null,
    asset_address   varchar(256),
    transaction_id  varchar(256)    not null,
    value           numeric not null,
    value_string    text    not null,
    value_scale     integer not null,
    address         varchar(256)    not null
);

create unique index balance_actions_natural_key_index_1
    on balance_actions (transaction_id, address, asset_id)   tablespace fast_space
    where asset_address is null;

create unique index balance_actions_natural_key_index_2
    on balance_actions (transaction_id, address, asset_id, asset_address)   tablespace fast_space
    where asset_address is not null;
    
create index balance_actions_block_id
    on balance_actions (block_id);

create index query_covered_by_address
    on balance_actions (address, block_number desc, asset_id, asset_address, value)  tablespace fast_space;

create  trigger set_numeric_value_from_string_trigger before insert or update
     on balance_actions for each row
     execute procedure set_numeric_value_from_string ();



create table assets
(
    id              varchar(328)                            not null,
    asset_id        varchar(64)                            not null,
    asset_address   varchar(256),
	scale int not null,
	constraint assets_natural_key_pk
		primary key (id)
);

COMMIT;

BEGIN;
create table block_headers
(
    number            bigint  not null,
    mined_at          timestamp with time zone     not null,
    size              integer not null,
    transaction_count integer not null,
    previous_block_id varchar(256),
    state             integer,
    id                varchar(256)    not null,
    constraint block_headers_natural_key_pk
        primary key (id)
);

create unique index block_headers_number_uindex
    on block_headers (number)   tablespace fast_space;

    
create index block_headers_mined_at_index
    on block_headers (mined_at)   tablespace fast_space;

create table chain_heads
(
    id                 varchar(128) not null,
    first_block_number bigint not null,
    block_number       bigint,
    mode               integer not null,
    mode_sequence      bigint not null,
    block_sequence     bigint not null,
    crawler_sequence   bigint not null,
    block_id           varchar(256),
    prev_block_id      varchar(256),
    constraint chain_heads_natural_key_pk
        primary key (id)
);

create table crawlers
(
    start_block           bigint not null,
    stop_accembling_block bigint not null,
    sequence              bigint not null,
    expected_block_number bigint not null,
    mode                  integer not null,
    constraint crawlers_natural_key_pk
        primary key (start_block, stop_accembling_block)
);

COMMIT;

create extension if not exists "uuid-ossp";

create table transactions
(
    id                 uuid default uuid_generate_v1() not null,
    block_id           varchar(256)                            not null,
    transaction_id     varchar(256)                            not null,
    transaction_number integer                         not null,
    type               integer                         not null,
    payload            jsonb                           not null,
    constraint transactions_pk
        primary key (id)
);

create index transactions_block_id_index
    on transactions (block_id) tablespace fast_space;

create unique index transactions_natural_key_index
    on transactions (transaction_id) tablespace fast_space;


COMMIT;
