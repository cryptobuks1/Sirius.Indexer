set search_path to @schemaName;

alter table unspent_coins
alter column script_pub_key type text;

alter table spent_coins
alter column script_pub_key type text;