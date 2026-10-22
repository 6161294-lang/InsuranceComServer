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
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NationalId = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
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
                name: "Claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClaimNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    InsuranceTypeId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HandlerId = table.Column<int>(type: "integer", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Claims_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Claims_Employees_HandlerId",
                        column: x => x.HandlerId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Claims_InsuranceTypes_InsuranceTypeId",
                        column: x => x.InsuranceTypeId,
                        principalTable: "InsuranceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClientInsuranceTypes",
                columns: table => new
                {
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    InsuranceTypeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientInsuranceTypes", x => new { x.ClientId, x.InsuranceTypeId });
                    table.ForeignKey(
                        name: "FK_ClientInsuranceTypes_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientInsuranceTypes_InsuranceTypes_InsuranceTypeId",
                        column: x => x.InsuranceTypeId,
                        principalTable: "InsuranceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Referrals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClaimId = table.Column<int>(type: "integer", nullable: false),
                    AppraiserId = table.Column<int>(type: "integer", nullable: false),
                    ReferredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Referrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Referrals_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Referrals_Employees_AppraiserId",
                        column: x => x.AppraiserId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Clients",
                columns: new[] { "Id", "Address", "Email", "FullName", "NationalId", "PasswordHash", "Phone" },
                values: new object[,]
                {
                    { 1, "הרצל 10, תל אביב", "client1@example.com", "ישראל ישראלי", "301234567", "$2a$11$KJArCNkktCpTLUeP3fXchOwkoRCTlP6KR1hKDhoYfDnjiCYdqqOL.", "050-1234567" },
                    { 2, "הנשיא 5, חיפה", "client2@example.com", "דנה כהן", "302345678", "$2a$11$KJArCNkktCpTLUeP3fXchOwkoRCTlP6KR1hKDhoYfDnjiCYdqqOL.", "052-7654321" }
                });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "Email", "FullName", "PasswordHash", "Role" },
                values: new object[,]
                {
                    { 1, "manager@example.com", "מנהל המערכת", "$2a$11$UljqLeoBVjHmKJEV7.472O4QB83DJJt2vPBtnnmv2b92O1mLms6Ha", "Manager" },
                    { 2, "appraiser1@example.com", "שמאי ראשון", "$2a$11$KL6TTo5sr0QrYwtN8vQVT.poPaW9ey0xXxNbvbUUWq4j0wq.Ntca2", "Appraiser" },
                    { 3, "appraiser2@example.com", "שמאי שני", "$2a$11$KL6TTo5sr0QrYwtN8vQVT.poPaW9ey0xXxNbvbUUWq4j0wq.Ntca2", "Appraiser" }
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
                table: "Claims",
                columns: new[] { "Id", "Amount", "AssignedAt", "ClaimNumber", "ClientId", "Description", "HandlerId", "InsuranceTypeId", "OpenedAt", "Status" },
                values: new object[,]
                {
                    { 1, 12500m, null, "CLM-0001", 1, "תאונה בצומת - פגיעה בדלת שמאל", null, 1, new DateTime(2026, 9, 1, 8, 0, 0, 0, DateTimeKind.Utc), "Opened" },
                    { 2, 2300m, null, "CLM-0002", 1, "שבר בשמשה הקדמית (טיוטה)", null, 1, new DateTime(2026, 9, 2, 9, 0, 0, 0, DateTimeKind.Utc), "Draft" },
                    { 3, 9500m, null, "CLM-0003", 1, "נזילה מהתקרה - נזק מים בסלון", null, 2, new DateTime(2026, 9, 3, 10, 0, 0, 0, DateTimeKind.Utc), "Opened" },
                    { 4, 7600m, new DateTime(2026, 9, 5, 9, 0, 0, 0, DateTimeKind.Utc), "CLM-0004", 1, "נזק מברד - שקעים בגג ובמכסה המנוע", 2, 1, new DateTime(2026, 9, 4, 11, 0, 0, 0, DateTimeKind.Utc), "InTreatment" },
                    { 5, 18900m, null, "CLM-0005", 2, "התנגשות מאחור בכביש מהיר", null, 1, new DateTime(2026, 9, 6, 13, 0, 0, 0, DateTimeKind.Utc), "Opened" },
                    { 6, 35000m, null, "CLM-0006", 2, "החזר על ניתוח ברך פרטי", null, 3, new DateTime(2026, 9, 7, 14, 0, 0, 0, DateTimeKind.Utc), "Opened" },
                    { 7, 4100m, new DateTime(2026, 9, 9, 9, 0, 0, 0, DateTimeKind.Utc), "CLM-0007", 2, "פגיעה בחניה - רכב פגע ונמלט", 3, 1, new DateTime(2026, 9, 8, 15, 0, 0, 0, DateTimeKind.Utc), "Approved" },
                    { 8, 1800m, new DateTime(2026, 9, 11, 9, 0, 0, 0, DateTimeKind.Utc), "CLM-0008", 2, "גניבת שתי מראות צד", 2, 1, new DateTime(2026, 9, 10, 16, 0, 0, 0, DateTimeKind.Utc), "Rejected" },
                    { 9, 6400m, null, "CLM-0009", 2, "החזר על תרופה שאינה בסל", null, 3, new DateTime(2026, 9, 12, 17, 0, 0, 0, DateTimeKind.Utc), "Cancelled" }
                });

            migrationBuilder.InsertData(
                table: "ClientInsuranceTypes",
                columns: new[] { "ClientId", "InsuranceTypeId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 1, 2 },
                    { 2, 1 },
                    { 2, 3 }
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
                table: "Referrals",
                columns: new[] { "Id", "AppraiserId", "ClaimId", "Notes", "ReferredAt" },
                values: new object[,]
                {
                    { 1, 2, 4, "נקבעה פגישה לבדיקת הרכב", new DateTime(2026, 9, 5, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 3, 7, "הנזק אומת - מומלץ לאשר", new DateTime(2026, 9, 9, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, 2, 8, "לא נמצא תיעוד לגניבה", new DateTime(2026, 9, 11, 9, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Claims_ClaimNumber",
                table: "Claims",
                column: "ClaimNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Claims_ClientId",
                table: "Claims",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_HandlerId",
                table: "Claims",
                column: "HandlerId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_InsuranceTypeId",
                table: "Claims",
                column: "InsuranceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Status",
                table: "Claims",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ClientInsuranceTypes_InsuranceTypeId",
                table: "ClientInsuranceTypes",
                column: "InsuranceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Email",
                table: "Clients",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_NationalId",
                table: "Clients",
                column: "NationalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email",
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
                name: "IX_Referrals_AppraiserId",
                table: "Referrals",
                column: "AppraiserId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ClaimId",
                table: "Referrals",
                column: "ClaimId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientInsuranceTypes");

            migrationBuilder.DropTable(
                name: "EmployeeSpecializations");

            migrationBuilder.DropTable(
                name: "Referrals");

            migrationBuilder.DropTable(
                name: "Claims");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "InsuranceTypes");
        }
    }
}
