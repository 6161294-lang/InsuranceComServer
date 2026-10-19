using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InsuranceClaims.Data.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSpecializations",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    InsuranceTypeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSpecializations", x => new { x.EmployeeId, x.InsuranceTypeId });
                    table.ForeignKey(
                        name: "FK_EmployeeSpecializations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeSpecializations_InsuranceTypes_InsuranceTypeId",
                        column: x => x.InsuranceTypeId,
                        principalTable: "InsuranceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Policies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PolicyNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    HolderName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CoverageAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsuranceTypeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Policies_InsuranceTypes_InsuranceTypeId",
                        column: x => x.InsuranceTypeId,
                        principalTable: "InsuranceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PolicyId = table.Column<int>(type: "integer", nullable: false),
                    HandlerId = table.Column<int>(type: "integer", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Claims_Employees_HandlerId",
                        column: x => x.HandlerId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Claims_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "FullName", "PasswordHash", "Role", "Username" },
                values: new object[,]
                {
                    { 1, "מנהל המערכת", "$2a$11$3IGB7AFLarhJ/LN33zo6r.OJ.w7HxDXqrLg4Io04kQL6Rjgcb6qzS", "Manager", "manager" },
                    { 2, "שמאי ראשון", "$2a$11$PPQUFfUgQaF182X5Eh12KeDNkCkta.TvZKVMvzOy4Nq.ykiI.6A8.", "Adjuster", "adjuster1" },
                    { 3, "שמאי שני", "$2a$11$PPQUFfUgQaF182X5Eh12KeDNkCkta.TvZKVMvzOy4Nq.ykiI.6A8.", "Adjuster", "adjuster2" }
                });

            migrationBuilder.InsertData(
                table: "InsuranceTypes",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "ביטוח מקיף, צד ג' וחובה", "ביטוח רכב" },
                    { 2, "מבנה ותכולה", "ביטוח דירה" },
                    { 3, "ניתוחים, תרופות ושירותים רפואיים", "ביטוח בריאות" }
                });

            migrationBuilder.InsertData(
                table: "EmployeeSpecializations",
                columns: new[] { "EmployeeId", "InsuranceTypeId" },
                values: new object[,]
                {
                    { 2, 1 },
                    { 2, 2 },
                    { 3, 1 },
                    { 3, 3 }
                });

            migrationBuilder.InsertData(
                table: "Policies",
                columns: new[] { "Id", "CoverageAmount", "EndDate", "HolderName", "InsuranceTypeId", "PolicyNumber", "StartDate" },
                values: new object[,]
                {
                    { 1, 150000m, new DateTime(2030, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "ישראל ישראלי", 1, "CAR-1001", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 120000m, new DateTime(2031, 5, 31, 0, 0, 0, 0, DateTimeKind.Utc), "דנה כהן", 1, "CAR-1002", new DateTime(2025, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, 900000m, new DateTime(2030, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "משה לוי", 2, "HOME-2001", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, 500000m, new DateTime(2030, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "רונית אברהם", 3, "HEALTH-3001", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, 80000m, new DateTime(2023, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "יוסי מזרחי", 1, "CAR-0999", new DateTime(2022, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Claims",
                columns: new[] { "Id", "Amount", "AssignedAt", "CreatedAt", "Description", "HandlerId", "PolicyId", "Status", "Title" },
                values: new object[,]
                {
                    { 1, 12500m, null, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), "פגיעה בדלת שמאל בצומת עזריאלי", null, 1, "Open", "תאונה בצומת" },
                    { 2, 2300m, null, new DateTime(2026, 9, 2, 9, 0, 0, 0, DateTimeKind.Utc), "אבן שהועפה מרכב בכביש 6", null, 1, "Open", "שבר שמשה קדמית" },
                    { 3, 4100m, null, new DateTime(2026, 9, 3, 10, 0, 0, 0, DateTimeKind.Utc), "רכב פגע ברכב החונה ונמלט", null, 2, "Open", "פגיעה בחניה" },
                    { 4, 1800m, null, new DateTime(2026, 9, 4, 11, 0, 0, 0, DateTimeKind.Utc), "שתי מראות צד נגנבו בלילה", null, 2, "Open", "גניבת מראות" },
                    { 5, 7600m, null, new DateTime(2026, 9, 5, 12, 0, 0, 0, DateTimeKind.Utc), "שקעים בגג ובמכסה המנוע", null, 1, "Open", "נזק מברד" },
                    { 6, 18900m, null, new DateTime(2026, 9, 6, 13, 0, 0, 0, DateTimeKind.Utc), "התנגשות מאחור בפקק", null, 2, "Open", "תאונה בכביש מהיר" },
                    { 7, 9500m, null, new DateTime(2026, 9, 7, 14, 0, 0, 0, DateTimeKind.Utc), "נזק מים בסלון מהשכנים מלמעלה", null, 3, "Open", "נזילה מהתקרה" },
                    { 8, 22000m, null, new DateTime(2026, 9, 8, 15, 0, 0, 0, DateTimeKind.Utc), "גניבת מחשב נייד ותכשיטים", null, 3, "Open", "פריצה לדירה" },
                    { 9, 35000m, null, new DateTime(2026, 9, 9, 16, 0, 0, 0, DateTimeKind.Utc), "החזר על ניתוח פרטי", null, 4, "Open", "ניתוח ברך" },
                    { 10, 6400m, null, new DateTime(2026, 9, 10, 17, 0, 0, 0, DateTimeKind.Utc), "החזר על תרופה לחצי שנה", null, 4, "Open", "תרופה שאינה בסל" },
                    { 11, 3200m, null, new DateTime(2026, 9, 11, 8, 0, 0, 0, DateTimeKind.Utc), "שריטה לאורך שתי דלתות", null, 1, "Open", "שריטות בצד ימין" },
                    { 12, 950m, null, new DateTime(2026, 9, 12, 9, 0, 0, 0, DateTimeKind.Utc), "פגיעה ברוורס בחניון", null, 2, "Open", "פנס אחורי שבור" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Claims_HandlerId",
                table: "Claims",
                column: "HandlerId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_PolicyId",
                table: "Claims",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Status",
                table: "Claims",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Username",
                table: "Employees",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSpecializations_InsuranceTypeId",
                table: "EmployeeSpecializations",
                column: "InsuranceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceTypes_Name",
                table: "InsuranceTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Policies_InsuranceTypeId",
                table: "Policies",
                column: "InsuranceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_PolicyNumber",
                table: "Policies",
                column: "PolicyNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Claims");

            migrationBuilder.DropTable(
                name: "EmployeeSpecializations");

            migrationBuilder.DropTable(
                name: "Policies");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "InsuranceTypes");
        }
    }
}
