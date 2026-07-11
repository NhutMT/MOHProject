using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MOHProject.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Insureds",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Residency = table.Column<int>(type: "int", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Insureds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Residency = table.Column<int>(type: "int", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PolicyHolders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Residency = table.Column<int>(type: "int", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyHolders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PremiumCollections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BaseCollected_Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BaseCollected_Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    BaseToCollect_Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BaseToCollect_Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    LinkedRidersCollected_Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LinkedRidersCollected_Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    LinkedRidersToCollect_Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LinkedRidersToCollect_Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    UnallocatedCash_Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnallocatedCash_Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PremiumCollections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UWStates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RcmpFlag = table.Column<bool>(type: "bit", nullable: false),
                    RcmpFlagEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AcceptCloa = table.Column<int>(type: "int", nullable: false),
                    AcceptCloaEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RcmpOption = table.Column<int>(type: "int", nullable: false),
                    RcmpOptionEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CompleteUw = table.Column<bool>(type: "bit", nullable: false),
                    CurrentComposition = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UWStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Policies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Substatus = table.Column<int>(type: "int", nullable: false),
                    InsuredResidency = table.Column<int>(type: "int", nullable: false),
                    PayerResidency = table.Column<int>(type: "int", nullable: false),
                    UwCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    UwStateId = table.Column<long>(type: "bigint", nullable: true),
                    PremiumCollectionId = table.Column<long>(type: "bigint", nullable: true),
                    InsuredId = table.Column<long>(type: "bigint", nullable: true),
                    PayerId = table.Column<long>(type: "bigint", nullable: true),
                    PolicyHolderId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Policies_Insureds_InsuredId",
                        column: x => x.InsuredId,
                        principalTable: "Insureds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Policies_Payers_PayerId",
                        column: x => x.PayerId,
                        principalTable: "Payers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Policies_PolicyHolders_PolicyHolderId",
                        column: x => x.PolicyHolderId,
                        principalTable: "PolicyHolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Policies_PremiumCollections_PremiumCollectionId",
                        column: x => x.PremiumCollectionId,
                        principalTable: "PremiumCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Policies_UWStates_UwStateId",
                        column: x => x.UwStateId,
                        principalTable: "UWStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyId = table.Column<long>(type: "bigint", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEntries_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Letters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Letters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Letters_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyId = table.Column<long>(type: "bigint", nullable: false),
                    IsBase = table.Column<bool>(type: "bit", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    HasActiveRiskLoading = table.Column<bool>(type: "bit", nullable: false),
                    HasActiveExclusion = table.Column<bool>(type: "bit", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusChangedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSelectedInProductTab = table.Column<bool>(type: "bit", nullable: false),
                    GrossPremium_Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrossPremium_Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    PrivateInsuranceExtraPremium_Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrivateInsuranceExtraPremium_Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plans_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reminders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyId = table.Column<long>(type: "bigint", nullable: false),
                    ParentLetterId = table.Column<long>(type: "bigint", nullable: false),
                    ReminderType = table.Column<int>(type: "int", nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reminders_Letters_ParentLetterId",
                        column: x => x.ParentLetterId,
                        principalTable: "Letters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reminders_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LetterPlans",
                columns: table => new
                {
                    LetterId = table.Column<long>(type: "bigint", nullable: false),
                    PlanId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LetterPlans", x => new { x.LetterId, x.PlanId });
                    table.ForeignKey(
                        name: "FK_LetterPlans_Letters_LetterId",
                        column: x => x.LetterId,
                        principalTable: "Letters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LetterPlans_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_PolicyId_OccurredAt",
                table: "AuditEntries",
                columns: new[] { "PolicyId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LetterPlans_PlanId",
                table: "LetterPlans",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Letters_PolicyId_Type_IsCurrent",
                table: "Letters",
                columns: new[] { "PolicyId", "Type", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_Plans_PolicyId",
                table: "Plans",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_InsuredId",
                table: "Policies",
                column: "InsuredId");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_PayerId",
                table: "Policies",
                column: "PayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_PolicyHolderId",
                table: "Policies",
                column: "PolicyHolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_PolicyNumber",
                table: "Policies",
                column: "PolicyNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Policies_PremiumCollectionId",
                table: "Policies",
                column: "PremiumCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_UwStateId",
                table: "Policies",
                column: "UwStateId");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_ParentLetterId",
                table: "Reminders",
                column: "ParentLetterId");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_PolicyId_Status",
                table: "Reminders",
                columns: new[] { "PolicyId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntries");

            migrationBuilder.DropTable(
                name: "LetterPlans");

            migrationBuilder.DropTable(
                name: "Reminders");

            migrationBuilder.DropTable(
                name: "Plans");

            migrationBuilder.DropTable(
                name: "Letters");

            migrationBuilder.DropTable(
                name: "Policies");

            migrationBuilder.DropTable(
                name: "Insureds");

            migrationBuilder.DropTable(
                name: "Payers");

            migrationBuilder.DropTable(
                name: "PolicyHolders");

            migrationBuilder.DropTable(
                name: "PremiumCollections");

            migrationBuilder.DropTable(
                name: "UWStates");
        }
    }
}
