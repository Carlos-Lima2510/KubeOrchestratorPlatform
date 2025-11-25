using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Servidor.Migrations
{
    /// <inheritdoc />
    public partial class UserClusterModelImplementation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClusterCredentials_ConnectedCluster_ConnectedClusterId",
                table: "ClusterCredentials");

            migrationBuilder.DropForeignKey(
                name: "FK_ConnectedCluster_ClusterCredentials_CredentialsId",
                table: "ConnectedCluster");

            migrationBuilder.DropForeignKey(
                name: "FK_ConnectedCluster_Users_UserId",
                table: "ConnectedCluster");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ConnectedCluster",
                table: "ConnectedCluster");

            migrationBuilder.RenameTable(
                name: "ConnectedCluster",
                newName: "ConnectedClusters");

            migrationBuilder.RenameIndex(
                name: "IX_ConnectedCluster_UserId_ClusterId_Context",
                table: "ConnectedClusters",
                newName: "IX_ConnectedClusters_UserId_ClusterId_Context");

            migrationBuilder.RenameIndex(
                name: "IX_ConnectedCluster_CredentialsId",
                table: "ConnectedClusters",
                newName: "IX_ConnectedClusters_CredentialsId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConnectedClusters",
                table: "ConnectedClusters",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClusterCredentials_ConnectedClusters_ConnectedClusterId",
                table: "ClusterCredentials",
                column: "ConnectedClusterId",
                principalTable: "ConnectedClusters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConnectedClusters_ClusterCredentials_CredentialsId",
                table: "ConnectedClusters",
                column: "CredentialsId",
                principalTable: "ClusterCredentials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConnectedClusters_Users_UserId",
                table: "ConnectedClusters",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClusterCredentials_ConnectedClusters_ConnectedClusterId",
                table: "ClusterCredentials");

            migrationBuilder.DropForeignKey(
                name: "FK_ConnectedClusters_ClusterCredentials_CredentialsId",
                table: "ConnectedClusters");

            migrationBuilder.DropForeignKey(
                name: "FK_ConnectedClusters_Users_UserId",
                table: "ConnectedClusters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ConnectedClusters",
                table: "ConnectedClusters");

            migrationBuilder.RenameTable(
                name: "ConnectedClusters",
                newName: "ConnectedCluster");

            migrationBuilder.RenameIndex(
                name: "IX_ConnectedClusters_UserId_ClusterId_Context",
                table: "ConnectedCluster",
                newName: "IX_ConnectedCluster_UserId_ClusterId_Context");

            migrationBuilder.RenameIndex(
                name: "IX_ConnectedClusters_CredentialsId",
                table: "ConnectedCluster",
                newName: "IX_ConnectedCluster_CredentialsId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConnectedCluster",
                table: "ConnectedCluster",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClusterCredentials_ConnectedCluster_ConnectedClusterId",
                table: "ClusterCredentials",
                column: "ConnectedClusterId",
                principalTable: "ConnectedCluster",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConnectedCluster_ClusterCredentials_CredentialsId",
                table: "ConnectedCluster",
                column: "CredentialsId",
                principalTable: "ClusterCredentials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConnectedCluster_Users_UserId",
                table: "ConnectedCluster",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
