set search_path to @schemaName;

alter table unspent_coins
drop column script_pub_key cascade;

alter table spent_coins
drop column script_pub_key cascade;