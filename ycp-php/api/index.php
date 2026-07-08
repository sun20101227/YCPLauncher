<?php
/**
 * YACHIYO CUP — REST API
 * GET  /api/download   → download info JSON
 * GET  /api/stats      → platform stats JSON
 * GET  /api/matches    → live match data JSON
 */
require_once __DIR__ . '/../db.php';
header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');

$action = $_GET['action'] ?? 'download';

switch ($action) {
    case 'download':
        echo json_encode([
            'version' => get_setting('download_ver', 'V2.1.0'),
            'url'     => get_setting('download_url'),
            'size'    => get_setting('download_size', '45 MB'),
        ]);
        break;

    case 'stats':
        echo json_encode(get_stats());
        break;

    case 'matches':
        echo json_encode(get_matches());
        break;

    default:
        http_response_code(404);
        echo json_encode(['error' => 'Unknown action']);
}
