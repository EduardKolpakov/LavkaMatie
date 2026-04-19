using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KLALIK.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "collection_directions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_collection_directions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "qualification_levels",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qualification_levels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workshop_services",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    image_asset_path = table.Column<string>(type: "text", nullable: true),
                    collection_direction_id = table.Column<int>(type: "integer", nullable: false),
                    service_category_id = table.Column<int>(type: "integer", nullable: false),
                    is_holiday_related = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workshop_services", x => x.id);
                    table.ForeignKey(
                        name: "fk_workshop_services_collection_directions_collection_directio",
                        column: x => x.collection_direction_id,
                        principalTable: "collection_directions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_workshop_services_service_categories_service_category_id",
                        column: x => x.service_category_id,
                        principalTable: "service_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "balance_transactions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    transaction_type = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_balance_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_balance_transactions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "master_profiles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    qualification_level_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_master_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_master_profiles_qualification_levels_qualification_level_id",
                        column: x => x.qualification_level_id,
                        principalTable: "qualification_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_master_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    client_user_id = table.Column<int>(type: "integer", nullable: false),
                    master_profile_id = table.Column<int>(type: "integer", nullable: false),
                    workshop_service_id = table.Column<int>(type: "integer", nullable: false),
                    queue_number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    scheduled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookings", x => x.id);
                    table.ForeignKey(
                        name: "fk_bookings_master_profiles_master_profile_id",
                        column: x => x.master_profile_id,
                        principalTable: "master_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_bookings_users_client_user_id",
                        column: x => x.client_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_bookings_workshop_services_workshop_service_id",
                        column: x => x.workshop_service_id,
                        principalTable: "workshop_services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "master_service_links",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    master_profile_id = table.Column<int>(type: "integer", nullable: false),
                    workshop_service_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_master_service_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_master_service_links_master_profiles_master_profile_id",
                        column: x => x.master_profile_id,
                        principalTable: "master_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_master_service_links_workshop_services_workshop_service_id",
                        column: x => x.workshop_service_id,
                        principalTable: "workshop_services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "qualification_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    master_profile_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolver_user_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qualification_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_qualification_requests_master_profiles_master_profile_id",
                        column: x => x.master_profile_id,
                        principalTable: "master_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_qualification_requests_users_resolver_user_id",
                        column: x => x.resolver_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    client_user_id = table.Column<int>(type: "integer", nullable: false),
                    workshop_service_id = table.Column<int>(type: "integer", nullable: true),
                    master_profile_id = table.Column<int>(type: "integer", nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviews", x => x.id);
                    table.CheckConstraint("ck_review_target", "\"workshop_service_id\" IS NOT NULL OR \"master_profile_id\" IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_reviews_master_profiles_master_profile_id",
                        column: x => x.master_profile_id,
                        principalTable: "master_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_reviews_users_client_user_id",
                        column: x => x.client_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_reviews_workshop_services_workshop_service_id",
                        column: x => x.workshop_service_id,
                        principalTable: "workshop_services",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_balance_transactions_user_id",
                table: "balance_transactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_client_user_id",
                table: "bookings",
                column: "client_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_master_profile_id",
                table: "bookings",
                column: "master_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_workshop_service_id",
                table: "bookings",
                column: "workshop_service_id");

            migrationBuilder.CreateIndex(
                name: "ix_collection_directions_name",
                table: "collection_directions",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_master_profiles_qualification_level_id",
                table: "master_profiles",
                column: "qualification_level_id");

            migrationBuilder.CreateIndex(
                name: "ix_master_profiles_user_id",
                table: "master_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_master_service_links_master_profile_id_workshop_service_id",
                table: "master_service_links",
                columns: new[] { "master_profile_id", "workshop_service_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_master_service_links_workshop_service_id",
                table: "master_service_links",
                column: "workshop_service_id");

            migrationBuilder.CreateIndex(
                name: "ix_qualification_levels_sort_order",
                table: "qualification_levels",
                column: "sort_order");

            migrationBuilder.CreateIndex(
                name: "ix_qualification_requests_master_profile_id",
                table: "qualification_requests",
                column: "master_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_qualification_requests_resolver_user_id",
                table: "qualification_requests",
                column: "resolver_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_client_user_id",
                table: "reviews",
                column: "client_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_master_profile_id",
                table: "reviews",
                column: "master_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_workshop_service_id",
                table: "reviews",
                column: "workshop_service_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_categories_name",
                table: "service_categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_role_id",
                table: "users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_workshop_services_collection_direction_id",
                table: "workshop_services",
                column: "collection_direction_id");

            migrationBuilder.CreateIndex(
                name: "ix_workshop_services_service_category_id",
                table: "workshop_services",
                column: "service_category_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "balance_transactions");

            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "master_service_links");

            migrationBuilder.DropTable(
                name: "qualification_requests");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "master_profiles");

            migrationBuilder.DropTable(
                name: "workshop_services");

            migrationBuilder.DropTable(
                name: "qualification_levels");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "collection_directions");

            migrationBuilder.DropTable(
                name: "service_categories");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
