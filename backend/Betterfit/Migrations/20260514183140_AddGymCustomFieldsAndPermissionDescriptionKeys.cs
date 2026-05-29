using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betterfit.Migrations
{
    /// <inheritdoc />
    public partial class AddGymCustomFieldsAndPermissionDescriptionKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Description",
                table: "PermissionCatalogItems",
                newName: "DescriptionKey");

            migrationBuilder.CreateTable(
                name: "GymCustomFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GymId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ValueType = table.Column<string>(type: "text", nullable: false),
                    OptionsJson = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymCustomFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymCustomFieldDefinitions_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GymCustomFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GymId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GymMembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymCustomFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymCustomFieldValues_GymCustomFieldDefinitions_FieldDefinit~",
                        column: x => x.FieldDefinitionId,
                        principalTable: "GymCustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GymCustomFieldValues_GymMemberships_GymMembershipId",
                        column: x => x.GymMembershipId,
                        principalTable: "GymMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GymCustomFieldValues_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("0995f6e7-d10b-486c-8633-b1a6140ea170"),
                column: "DescriptionKey",
                value: "WRITE_LOCATIONS");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("1015f36b-e0c6-4cd7-88fc-3b2e25572c80"),
                column: "DescriptionKey",
                value: "READ_GYMS");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("132f89ad-c2de-4f84-93af-8619d30fca0d"),
                column: "DescriptionKey",
                value: "WRITE_WORKOUTS");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("27ecb0f0-4862-45eb-9b9f-2ca9505855f4"),
                column: "DescriptionKey",
                value: "APPROVE_PLANS");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("32291f01-7fe4-44cf-9f1c-82bbf8a37295"),
                column: "DescriptionKey",
                value: "READ_MEMBERS");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("4f0409b0-c0f2-47cb-94dd-722efcc5e847"),
                column: "DescriptionKey",
                value: "READ_CLASSES");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("54e4a2dd-8e6d-4919-b7d6-dd5b7b8ed5f7"),
                column: "DescriptionKey",
                value: "WRITE_SECURITY_POLICY");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("5adf6f11-4129-4333-a76e-a4c752d8fd54"),
                column: "DescriptionKey",
                value: "WRITE_INTEGRATIONS");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("6fcf5996-b724-44aa-bab7-7754505dcf3e"),
                column: "DescriptionKey",
                value: "READ_SECURITY_POLICY");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("75d7dd0d-9997-4f7a-8258-8a98b2e8717f"),
                column: "DescriptionKey",
                value: "WRITE_CRM");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("7689fa0a-38ae-4d10-b8fc-e6db96ca15ec"),
                column: "DescriptionKey",
                value: "READ_REPORTS");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("93a83f38-69ef-4637-ae5f-7ec42b17dcb6"),
                column: "DescriptionKey",
                value: "WRITE_PROFILE");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("98b12021-bad0-49ec-bd1f-2d8d4e9b3f81"),
                column: "DescriptionKey",
                value: "WRITE_STAFF_ASSIGNMENTS");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("99727c60-2d9b-4c92-bc55-d1cc633a09f9"),
                column: "DescriptionKey",
                value: "READ_CRM");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("9d56e44c-0117-4f5c-9c53-33257c380389"),
                column: "DescriptionKey",
                value: "WRITE_GYMS");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("a5c55147-1d99-433e-b022-5164d7eff76d"),
                column: "DescriptionKey",
                value: "APPROVE_CHECKINS");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("aa09dba0-1f46-4197-a8e2-445c10d737f6"),
                column: "DescriptionKey",
                value: "READ_ROLES");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("bbd8883e-d299-41ef-8fd7-e39db8a9dab4"),
                column: "DescriptionKey",
                value: "READ_INTEGRATIONS");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("c04e4eef-c5f7-42b0-a820-c50a3978d19f"),
                column: "DescriptionKey",
                value: "WRITE_BILLING");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("c7c46676-f643-4d17-b541-7d81dcbca635"),
                column: "DescriptionKey",
                value: "READ_BILLING");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("c8e7699f-f64f-4578-9a8a-e486e3fd7f95"),
                column: "DescriptionKey",
                value: "WRITE_CLASSES");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("d88d6e31-2f85-4d05-a9bc-802eec0e7846"),
                column: "DescriptionKey",
                value: "READ_LOCATIONS");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("de2a4fcb-13dd-4a8f-b435-5f1f0ca10961"),
                column: "DescriptionKey",
                value: "READ_PROFILE");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("e00a4ee3-ec77-4030-8df3-31616b19226a"),
                column: "DescriptionKey",
                value: "EXPORT_REPORTS");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("ea2b1144-a8f9-4f15-8afa-4fcf60ecaf50"),
                column: "DescriptionKey",
                value: "READ_WORKOUTS");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("f2895883-e3b6-4f31-95ba-e8aa183d9a31"),
                column: "DescriptionKey",
                value: "WRITE_MEMBERS");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("f3dce9e8-6d98-40df-a312-3152f3248e09"),
                column: "DescriptionKey",
                value: "WRITE_ROLES");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("f4146dab-43bc-494c-86d2-c0256744996a"),
                column: "DescriptionKey",
                value: "READ_STAFF_ASSIGNMENTS");

            migrationBuilder.CreateIndex(
                name: "IX_GymCustomFieldDefinitions_GymId_EntityType_IsActive_SortOrd~",
                table: "GymCustomFieldDefinitions",
                columns: new[] { "GymId", "EntityType", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_GymCustomFieldDefinitions_GymId_EntityType_Key",
                table: "GymCustomFieldDefinitions",
                columns: new[] { "GymId", "EntityType", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GymCustomFieldValues_FieldDefinitionId",
                table: "GymCustomFieldValues",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_GymCustomFieldValues_GymId_FieldDefinitionId",
                table: "GymCustomFieldValues",
                columns: new[] { "GymId", "FieldDefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_GymCustomFieldValues_GymMembershipId_FieldDefinitionId",
                table: "GymCustomFieldValues",
                columns: new[] { "GymMembershipId", "FieldDefinitionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GymCustomFieldValues");

            migrationBuilder.DropTable(
                name: "GymCustomFieldDefinitions");

            migrationBuilder.RenameColumn(
                name: "DescriptionKey",
                table: "PermissionCatalogItems",
                newName: "Description");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("0995f6e7-d10b-486c-8633-b1a6140ea170"),
                column: "Description",
                value: "Create or update gym locations");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("1015f36b-e0c6-4cd7-88fc-3b2e25572c80"),
                column: "Description",
                value: "View tenant records");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("132f89ad-c2de-4f84-93af-8619d30fca0d"),
                column: "Description",
                value: "Create or update workout plans");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("27ecb0f0-4862-45eb-9b9f-2ca9505855f4"),
                column: "Description",
                value: "Approve plans and subscriptions");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("32291f01-7fe4-44cf-9f1c-82bbf8a37295"),
                column: "Description",
                value: "View member data");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("4f0409b0-c0f2-47cb-94dd-722efcc5e847"),
                column: "Description",
                value: "View class schedules");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("54e4a2dd-8e6d-4919-b7d6-dd5b7b8ed5f7"),
                column: "Description",
                value: "Create or update tenant authentication and security policy");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("5adf6f11-4129-4333-a76e-a4c752d8fd54"),
                column: "Description",
                value: "Create or update external integrations");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("6fcf5996-b724-44aa-bab7-7754505dcf3e"),
                column: "Description",
                value: "View tenant authentication and security policy");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("75d7dd0d-9997-4f7a-8258-8a98b2e8717f"),
                column: "Description",
                value: "Create or update leads and commercial tasks");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("7689fa0a-38ae-4d10-b8fc-e6db96ca15ec"),
                column: "Description",
                value: "View reports and KPI dashboards");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("93a83f38-69ef-4637-ae5f-7ec42b17dcb6"),
                column: "Description",
                value: "Update own profile");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("98b12021-bad0-49ec-bd1f-2d8d4e9b3f81"),
                column: "Description",
                value: "Create or update tenant staff assignments");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("99727c60-2d9b-4c92-bc55-d1cc633a09f9"),
                column: "Description",
                value: "View leads and commercial tasks");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("9d56e44c-0117-4f5c-9c53-33257c380389"),
                column: "Description",
                value: "Create or update tenant records");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("a5c55147-1d99-433e-b022-5164d7eff76d"),
                column: "Description",
                value: "Approve member check-ins");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("aa09dba0-1f46-4197-a8e2-445c10d737f6"),
                column: "Description",
                value: "View role definitions");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("bbd8883e-d299-41ef-8fd7-e39db8a9dab4"),
                column: "Description",
                value: "View external integrations and connection status");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("c04e4eef-c5f7-42b0-a820-c50a3978d19f"),
                column: "Description",
                value: "Modify billing information");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("c7c46676-f643-4d17-b541-7d81dcbca635"),
                column: "Description",
                value: "View billing information");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("c8e7699f-f64f-4578-9a8a-e486e3fd7f95"),
                column: "Description",
                value: "Create or update class schedules");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("d88d6e31-2f85-4d05-a9bc-802eec0e7846"),
                column: "Description",
                value: "View gym locations");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("de2a4fcb-13dd-4a8f-b435-5f1f0ca10961"),
                column: "Description",
                value: "View own profile");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("e00a4ee3-ec77-4030-8df3-31616b19226a"),
                column: "Description",
                value: "Export reports");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("ea2b1144-a8f9-4f15-8afa-4fcf60ecaf50"),
                column: "Description",
                value: "View workout plans");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("f2895883-e3b6-4f31-95ba-e8aa183d9a31"),
                column: "Description",
                value: "Create or update members");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("f3dce9e8-6d98-40df-a312-3152f3248e09"),
                column: "Description",
                value: "Create or update role definitions");

            migrationBuilder.UpdateData(
                table: "PermissionCatalogItems",
                keyColumn: "Id",
                keyValue: new Guid("f4146dab-43bc-494c-86d2-c0256744996a"),
                column: "Description",
                value: "View tenant staff assignments");
        }
    }
}
