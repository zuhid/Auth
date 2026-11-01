#!/bin/bash

################################################## variables ##################################################
postgres_image="postgis/postgis:16-3.5"
postgres_container="postgres"
postgres_user="postgres"
postgres_password="P@ssw0rd"
dbuser_user="dbuser"
dbuser_password="P@ssw0rd"

################################################## functions ##################################################

rebuild_postgres_server(){
  # remove the image
  docker container rm "$postgres_container" --force
  # Run the PostgreSQL container
  docker run --name $postgres_container --publish 5432:5432 --detach \
    --env "POSTGRES_USER=$postgres_user" \
    --env "POSTGRES_PASSWORD=$postgres_password" \
    "$postgres_image"
  # sleep for a few seconds to let the docker service start up
  sleep 5
  # create user 'dbuser'
  docker exec -it $postgres_container psql -U $postgres_user -c "create user dbuser with superuser password 'P@ssw0rd';"
}

recreate_database(){
  # get the database in lower case
  dbname=${1,,}
  # kill all connection
  docker exec -it $postgres_container psql -U $postgres_user -c "
  revoke connect on database $dbname from public;
  select pg_terminate_backend(pid)
  from pg_stat_activity
  where datname = '$dbname'
    and pid <> pg_backend_pid();
  "
  # drop the database
  docker exec -it $postgres_container psql -U $postgres_user -c "drop database if exists $dbname;"
  # create the database
  docker exec -it $postgres_container psql -U $postgres_user -c "create database $dbname;"
  # create the __EFMigrationsHistory table
  docker exec -it $postgres_container psql -U $postgres_user -d $dbname -c '
  create table if not exists "__EFMigrationsHistory" (
    "MigrationId" character varying(150) not null,
    "PostgresTypeVersion" character varying(32) not null,
    constraint "PK___EFMigrationsHistory" primary key ("MigrationId")
  );'
}

run_test() {
  project=$1
  rm -rf $project/TestResults
  dotnet test $project --settings $project/coverlet.runsettings --collect:"XPlat Code Coverage"
  dotnet reportgenerator -reports:"$project/TestResults/*/coverage.cobertura.xml" -targetdir:"$project/TestResults/CoverageReport" -reporttypes:Html
  echo "$PWD/$project/TestResults/CoverageReport/index.html"

  firefox "$PWD/$project/TestResults/CoverageReport/index.html" &
  # google-chrome "$PWD/$project/TestResults/CoverageReport/index.html" &
  # /opt/microsoft/msedge/msedge "$PWD/$project/TestResults/CoverageReport/index.html" &

  # "/c/Program Files/Mozilla Firefox/firefox.exe" "$PWD/$project/TestResults/CoverageReport/index.html" &
  # "/c/Program Files/Google/Chrome/Application/chrome.exe" "$PWD/$project/TestResults/CoverageReport/index.html" &
}

publish_docker() {
  file=$1
  tag=$2
  docker build --tag tzather/$tag --file $file .
  docker push tzather/$tag # push container https://hub.docker.com/u/tzather
}

################################################## execute ##################################################
clear
docker

# docker start $postgres_container
# docker start mailpit
# rebuild_postgres_server
# update_dotnet_packages
# solution_initilize

# recreate_database "identity_csharp"
# update_database "Api.Identity" "Api.Identity"
run_test "Api.Identity.Tests"
# publish_docker Identity/Dockerfile Identity
