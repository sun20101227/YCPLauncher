<?php
/**
 * YACHIYO CUP — Secure Chunk Upload Receiver
 * ============================================
 * 接收浏览器分片，拼合成完整文件，并可管理已上传文件。
 *
 * ⚠️  上线前必改：把下方密码改成你自己的！
 */

// 引入数据库层（版本号/设置管理）
$db_path = __DIR__ . '/db.php';
if (file_exists($db_path)) require_once $db_path;

define('UPLOAD_PASSWORD', 'YCP@2026Admin');   // ← 必改！
define('UPLOAD_DIR',      __DIR__ . '/dist/');  // 文件存放目录
define('TEMP_DIR',        __DIR__ . '/tmp_chunks/'); // 临时分片目录
define('MAX_CHUNK_MB',    90);  // 单片最大 MB（必须 < Cloudflare 100MB 限制）

// ── 跨域 & JSON 头 ─────────────────────────────────────────
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Headers: X-Auth, Content-Type');
header('Content-Type: application/json; charset=utf-8');

if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') { http_response_code(204); exit; }

// ── 鉴权 ───────────────────────────────────────────────────
$auth = $_SERVER['HTTP_X_AUTH'] ?? ($_POST['auth'] ?? '');
if ($auth !== UPLOAD_PASSWORD) {
    http_response_code(403);
    echo json_encode(['ok' => false, 'msg' => '密码错误']);
    exit;
}

// ── 创建目录 ────────────────────────────────────────────────
foreach ([UPLOAD_DIR, TEMP_DIR] as $d) {
    if (!is_dir($d)) mkdir($d, 0755, true);
}

$action = $_POST['action'] ?? $_GET['action'] ?? '';

// ══════════════════════════════════════════════════════════════
// ACTION: upload_chunk — 接收单个分片
// ══════════════════════════════════════════════════════════════
if ($action === 'upload_chunk') {
    $uid       = preg_replace('/[^a-f0-9]/', '', $_POST['uid']       ?? '');
    $index     = (int)($_POST['chunk_index']  ?? -1);
    $total     = (int)($_POST['chunk_total']  ?? -1);
    $filename  = basename($_POST['filename']  ?? 'upload.bin');
    $filename  = preg_replace('/[^a-zA-Z0-9._\-\(\) ]/', '', $filename);

    if (!$uid || $index < 0 || $total < 1 || !$filename) {
        echo json_encode(['ok' => false, 'msg' => '参数错误']); exit;
    }

    // 验证分片大小
    $chunkSize = $_FILES['chunk']['size'] ?? 0;
    if ($chunkSize > MAX_CHUNK_MB * 1024 * 1024) {
        echo json_encode(['ok' => false, 'msg' => '分片超过 ' . MAX_CHUNK_MB . 'MB 限制']); exit;
    }

    // 保存分片
    $chunkPath = TEMP_DIR . $uid . '_' . $index;
    if (!move_uploaded_file($_FILES['chunk']['tmp_name'], $chunkPath)) {
        echo json_encode(['ok' => false, 'msg' => '分片保存失败']); exit;
    }

    // 检查是否所有分片都到齐
    $ready = true;
    for ($i = 0; $i < $total; $i++) {
        if (!file_exists(TEMP_DIR . $uid . '_' . $i)) { $ready = false; break; }
    }

    if ($ready) {
        // ── 合并所有分片 ──
        $outPath = UPLOAD_DIR . $filename;
        $out = fopen($outPath, 'wb');
        for ($i = 0; $i < $total; $i++) {
            $p = TEMP_DIR . $uid . '_' . $i;
            fwrite($out, file_get_contents($p));
            unlink($p); // 清理分片
        }
        fclose($out);

        $sizeMB = round(filesize($outPath) / 1048576, 1);
        echo json_encode([
            'ok'       => true,
            'done'     => true,
            'filename' => $filename,
            'size_mb'  => $sizeMB,
            'url'      => 'dist/' . rawurlencode($filename),
            'msg'      => "✅ 合并完成！{$filename}（{$sizeMB} MB）"
        ]);
    } else {
        echo json_encode([
            'ok'   => true,
            'done' => false,
            'msg'  => "分片 " . ($index + 1) . "/{$total} 已接收"
        ]);
    }
    exit;
}

// ══════════════════════════════════════════════════════════════
// ACTION: list_files — 列出已上传文件
// ══════════════════════════════════════════════════════════════
if ($action === 'list_files') {
    $files = [];
    foreach (glob(UPLOAD_DIR . '*') as $f) {
        if (is_file($f)) {
            $files[] = [
                'name'     => basename($f),
                'size_mb'  => round(filesize($f) / 1048576, 1),
                'modified' => date('Y-m-d H:i:s', filemtime($f)),
                'url'      => 'dist/' . rawurlencode(basename($f)),
            ];
        }
    }
    echo json_encode(['ok' => true, 'files' => $files]);
    exit;
}

// ══════════════════════════════════════════════════════════════
// ACTION: delete_file — 删除文件
// ══════════════════════════════════════════════════════════════
if ($action === 'delete_file') {
    $name = basename($_POST['name'] ?? '');
    $path = UPLOAD_DIR . $name;
    if ($name && file_exists($path)) {
        unlink($path);
        echo json_encode(['ok' => true, 'msg' => "已删除 {$name}"]);
    } else {
        echo json_encode(['ok' => false, 'msg' => '文件不存在']);
    }
    exit;
}

// ══════════════════════════════════════════════════════════════
// ACTION: get_settings — 读取所有可编辑设置
// ══════════════════════════════════════════════════════════════
if ($action === 'get_settings') {
    if (!function_exists('get_pdo')) {
        echo json_encode(['ok'=>false,'msg'=>'db.php 不存在，无法读取设置']); exit;
    }
    $rows = get_pdo()->query("SELECT k,v FROM settings")->fetchAll(PDO::FETCH_KEY_PAIR);
    echo json_encode(['ok'=>true,'settings'=>$rows]);
    exit;
}

// ══════════════════════════════════════════════════════════════
// ACTION: update_setting — 更新单条设置
// ══════════════════════════════════════════════════════════════
if ($action === 'update_setting') {
    if (!function_exists('get_pdo')) {
        echo json_encode(['ok'=>false,'msg'=>'db.php 不存在，无法保存设置']); exit;
    }
    $key = trim($_POST['key']   ?? '');
    $val = trim($_POST['value'] ?? '');
    $allowed = ['download_url','download_ver','download_size'];
    if (!in_array($key, $allowed)) {
        echo json_encode(['ok'=>false,'msg'=>'不允许修改该字段']); exit;
    }
    $stmt = get_pdo()->prepare("INSERT INTO settings(k,v) VALUES(?,?) ON CONFLICT(k) DO UPDATE SET v=excluded.v");
    $stmt->execute([$key,$val]);
    echo json_encode(['ok'=>true,'msg'=>"✅ {$key} 已更新"]);
    exit;
}

echo json_encode(['ok' => false, 'msg' => '未知操作']);
