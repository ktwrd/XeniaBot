using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XeniaDiscord.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateFuncBanSyncGetMutualRecordsForGuild : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
@"CREATE OR REPLACE FUNCTION public.""spBanSyncGetMutualRecordsForGuild""(IN ""TargetGuildId"" character varying(40))
    RETURNS SETOF ""BanSyncRecords""
    LANGUAGE 'plpgsql'
    
AS $BODY$
	BEGIN
		return query
		select bsr.* from ""BanSyncRecords"" bsr
		left outer join ""Cache_GuildMember"" cgm on cgm.""UserId"" = bsr.""UserId"" and cgm.""GuildId"" = $1
		where bsr.""GuildId"" = $1
		or cgm.""UserId"" is not null
        and bsr.""Ghost"" <> True;
	END
$BODY$;");
            migrationBuilder.Sql(
@"CREATE OR REPLACE FUNCTION public.""spBanSyncGetMutualRecordsForGuild_Paginate""(
		IN ""TargetGuildId"" character varying(40),
		IN ""Page"" integer,
		IN ""PageSize"" integer)
    RETURNS SETOF ""BanSyncRecords""
    LANGUAGE 'plpgsql'
    
AS $BODY$
	BEGIN
		return query
		select bsr.* from ""BanSyncRecords"" bsr
		left outer join ""Cache_GuildMember"" cgm on cgm.""UserId"" = bsr.""UserId"" and cgm.""GuildId"" = $1
		where bsr.""GuildId"" = $1
		or cgm.""UserId"" is not null
        and bsr.""Ghost"" <> True
		order by ""CreatedAt"" desc
		offset ($2 * $3)
		limit $3;
	END
$BODY$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP FUNCTION public.""spBanSyncGetMutualRecordsForGuild_Paginate""");
            migrationBuilder.Sql(@"DROP FUNCTION public.""spBanSyncGetMutualRecordsForGuild""");
        }
    }
}
