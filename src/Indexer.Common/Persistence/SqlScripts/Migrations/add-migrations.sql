set search_path to @schemaName;

create table migrations
(
    version        int not null,
    script         varchar(256) not null,
    date           timestamp not null,

    constraint pk_migrations primary key (version)
);