using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnvSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Path = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AASSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnvId = table.Column<int>(type: "integer", nullable: false),
                    IdShort = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Extensions = table.Column<string>(type: "text", nullable: true),
                    Identifier = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EmbeddedDataSpecifications = table.Column<string>(type: "text", nullable: true),
                    DerivedFrom = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    Revision = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    Creator = table.Column<string>(type: "text", nullable: true),
                    TemplateId = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AEmbeddedDataSpecifications = table.Column<string>(type: "text", nullable: true),
                    AssetKind = table.Column<string>(type: "text", nullable: true),
                    GlobalAssetId = table.Column<string>(type: "text", nullable: true),
                    AssetType = table.Column<string>(type: "text", nullable: true),
                    SpecificAssetIds = table.Column<string>(type: "text", nullable: true),
                    DefaultThumbnailPath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DefaultThumbnailContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TimeStampCreate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeStampTree = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeStampDelete = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AASSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AASSets_EnvSets_EnvId",
                        column: x => x.EnvId,
                        principalTable: "EnvSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CDSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnvId = table.Column<int>(type: "integer", nullable: false),
                    IdShort = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Extensions = table.Column<string>(type: "text", nullable: true),
                    Identifier = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsCaseOf = table.Column<string>(type: "text", nullable: true),
                    EmbeddedDataSpecifications = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    Revision = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    Creator = table.Column<string>(type: "text", nullable: true),
                    TemplateId = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AEmbeddedDataSpecifications = table.Column<string>(type: "text", nullable: true),
                    TimeStampCreate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeStampTree = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeStampDelete = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CDSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CDSets_EnvSets_EnvId",
                        column: x => x.EnvId,
                        principalTable: "EnvSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SMSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnvId = table.Column<int>(type: "integer", nullable: false),
                    AASId = table.Column<int>(type: "integer", nullable: true),
                    IdShort = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Extensions = table.Column<string>(type: "text", nullable: true),
                    Identifier = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Kind = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    SemanticId = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SupplementalSemanticIds = table.Column<string>(type: "text", nullable: true),
                    Qualifiers = table.Column<string>(type: "text", nullable: true),
                    EmbeddedDataSpecifications = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    Revision = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    Creator = table.Column<string>(type: "text", nullable: true),
                    TemplateId = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AEmbeddedDataSpecifications = table.Column<string>(type: "text", nullable: true),
                    TimeStampCreate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeStampTree = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeStampDelete = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SMSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SMSets_AASSets_AASId",
                        column: x => x.AASId,
                        principalTable: "AASSets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SMSets_EnvSets_EnvId",
                        column: x => x.EnvId,
                        principalTable: "EnvSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SMESets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SMId = table.Column<int>(type: "integer", nullable: false),
                    ParentSMEId = table.Column<int>(type: "integer", nullable: true),
                    SMEType = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    IdShort = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Extensions = table.Column<string>(type: "text", nullable: true),
                    SemanticId = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SupplementalSemanticIds = table.Column<string>(type: "text", nullable: true),
                    Qualifiers = table.Column<string>(type: "text", nullable: true),
                    EmbeddedDataSpecifications = table.Column<string>(type: "text", nullable: true),
                    TValue = table.Column<string>(type: "char(1)", nullable: true),
                    TimeStampCreate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeStampTree = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeStampDelete = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SMESets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SMESets_SMESets_ParentSMEId",
                        column: x => x.ParentSMEId,
                        principalTable: "SMESets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SMESets_SMSets_SMId",
                        column: x => x.SMId,
                        principalTable: "SMSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SMEId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: true),
                    Annotation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DValueSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DValueSets_SMESets_SMEId",
                        column: x => x.SMEId,
                        principalTable: "SMESets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SMEId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<long>(type: "bigint", nullable: true),
                    Annotation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IValueSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IValueSets_SMESets_SMEId",
                        column: x => x.SMEId,
                        principalTable: "SMESets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SMEId = table.Column<int>(type: "integer", nullable: false),
                    Attribute = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OValueSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OValueSets_SMESets_SMEId",
                        column: x => x.SMEId,
                        principalTable: "SMESets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SMEId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true),
                    Annotation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SValueSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SValueSets_SMESets_SMEId",
                        column: x => x.SMEId,
                        principalTable: "SMESets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AASSets_EnvId",
                table: "AASSets",
                column: "EnvId");

            migrationBuilder.CreateIndex(
                name: "IX_AASSets_Id",
                table: "AASSets",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_CDSets_EnvId",
                table: "CDSets",
                column: "EnvId");

            migrationBuilder.CreateIndex(
                name: "IX_CDSets_Id",
                table: "CDSets",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_DValueSets_Id",
                table: "DValueSets",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_DValueSets_SMEId",
                table: "DValueSets",
                column: "SMEId");

            migrationBuilder.CreateIndex(
                name: "IX_DValueSets_Value",
                table: "DValueSets",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_EnvSets_Id",
                table: "EnvSets",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_IValueSets_Id",
                table: "IValueSets",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_IValueSets_SMEId",
                table: "IValueSets",
                column: "SMEId");

            migrationBuilder.CreateIndex(
                name: "IX_IValueSets_Value",
                table: "IValueSets",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_OValueSets_Id",
                table: "OValueSets",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_OValueSets_SMEId",
                table: "OValueSets",
                column: "SMEId");

            migrationBuilder.CreateIndex(
                name: "IX_OValueSets_Value",
                table: "OValueSets",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_SMESets_Id",
                table: "SMESets",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_SMESets_IdShort",
                table: "SMESets",
                column: "IdShort");

            migrationBuilder.CreateIndex(
                name: "IX_SMESets_ParentSMEId",
                table: "SMESets",
                column: "ParentSMEId");

            migrationBuilder.CreateIndex(
                name: "IX_SMESets_SemanticId",
                table: "SMESets",
                column: "SemanticId");

            migrationBuilder.CreateIndex(
                name: "IX_SMESets_SMId",
                table: "SMESets",
                column: "SMId");

            migrationBuilder.CreateIndex(
                name: "IX_SMESets_TimeStamp",
                table: "SMESets",
                column: "TimeStamp");

            migrationBuilder.CreateIndex(
                name: "IX_SMSets_AASId",
                table: "SMSets",
                column: "AASId");

            migrationBuilder.CreateIndex(
                name: "IX_SMSets_EnvId",
                table: "SMSets",
                column: "EnvId");

            migrationBuilder.CreateIndex(
                name: "IX_SMSets_Id",
                table: "SMSets",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_SMSets_Identifier",
                table: "SMSets",
                column: "Identifier");

            migrationBuilder.CreateIndex(
                name: "IX_SMSets_SemanticId",
                table: "SMSets",
                column: "SemanticId");

            migrationBuilder.CreateIndex(
                name: "IX_SMSets_TimeStampTree",
                table: "SMSets",
                column: "TimeStampTree");

            migrationBuilder.CreateIndex(
                name: "IX_SValueSets_Id",
                table: "SValueSets",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_SValueSets_SMEId",
                table: "SValueSets",
                column: "SMEId");

            migrationBuilder.CreateIndex(
                name: "IX_SValueSets_Value",
                table: "SValueSets",
                column: "Value");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CDSets");

            migrationBuilder.DropTable(
                name: "DValueSets");

            migrationBuilder.DropTable(
                name: "IValueSets");

            migrationBuilder.DropTable(
                name: "OValueSets");

            migrationBuilder.DropTable(
                name: "SValueSets");

            migrationBuilder.DropTable(
                name: "SMESets");

            migrationBuilder.DropTable(
                name: "SMSets");

            migrationBuilder.DropTable(
                name: "AASSets");

            migrationBuilder.DropTable(
                name: "EnvSets");
        }
    }
}
