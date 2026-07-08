<?php
/**
 * YACHIYO CUP — Database Layer
 * Supports SQLite (default, zero-config) or MySQL (set USE_MYSQL=true)
 */

define('USE_MYSQL', false);          // ← 切换为 true 启用 MySQL
define('MYSQL_HOST', 'localhost');
define('MYSQL_DB',   'ycp_db');
define('MYSQL_USER', 'root');
define('MYSQL_PASS', '');
define('SQLITE_PATH', __DIR__ . '/ycp.db');

function get_pdo(): PDO {
    static $pdo = null;
    if ($pdo) return $pdo;

    if (USE_MYSQL) {
        $dsn = 'mysql:host=' . MYSQL_HOST . ';dbname=' . MYSQL_DB . ';charset=utf8mb4';
        $pdo = new PDO($dsn, MYSQL_USER, MYSQL_PASS, [PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION]);
    } else {
        $pdo = new PDO('sqlite:' . SQLITE_PATH, null, null, [PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION]);
        $pdo->exec('PRAGMA journal_mode=WAL;');
    }
    init_schema($pdo);
    return $pdo;
}

function init_schema(PDO $pdo): void {
    $pdo->exec("
        CREATE TABLE IF NOT EXISTS settings (
            k   TEXT PRIMARY KEY,
            v   TEXT NOT NULL DEFAULT ''
        );
        CREATE TABLE IF NOT EXISTS stats (
            id        INTEGER PRIMARY KEY AUTOINCREMENT,
            value     TEXT    NOT NULL,
            label_zh  TEXT    NOT NULL,
            label_en  TEXT    NOT NULL,
            sort_order INTEGER NOT NULL DEFAULT 0
        );
        CREATE TABLE IF NOT EXISTS matches (
            id         INTEGER PRIMARY KEY AUTOINCREMENT,
            team1_name TEXT NOT NULL,
            team1_flag TEXT NOT NULL DEFAULT '',
            team2_name TEXT NOT NULL,
            team2_flag TEXT NOT NULL DEFAULT '',
            score1     INTEGER NOT NULL DEFAULT 0,
            score2     INTEGER NOT NULL DEFAULT 0,
            map_name   TEXT NOT NULL DEFAULT 'Unknown',
            status     TEXT NOT NULL DEFAULT 'LIVE',
            elapsed    TEXT NOT NULL DEFAULT '00:00'
        );
        CREATE TABLE IF NOT EXISTS features (
            id         INTEGER PRIMARY KEY AUTOINCREMENT,
            tag        TEXT    NOT NULL,
            title      TEXT    NOT NULL,
            description TEXT   NOT NULL,
            image      TEXT    NOT NULL,
            highlights TEXT    NOT NULL DEFAULT '',
            center_overlay INTEGER NOT NULL DEFAULT 0,
            sort_order INTEGER NOT NULL DEFAULT 0
        );
    ");
    seed_data($pdo);
}

function seed_data(PDO $pdo): void {
    // Only seed if tables are empty
    $count = $pdo->query('SELECT COUNT(*) FROM settings')->fetchColumn();
    if ($count > 0) return;

    // Settings
    $pdo->exec("INSERT INTO settings(k,v) VALUES
        ('download_url',  'https://github.com/yachiyo-cup/releases/latest/YCPLauncher-Setup.exe'),
        ('download_ver',  'V2.1.0'),
        ('download_size', '45 MB')
    ");

    // Stats
    $pdo->exec("INSERT INTO stats(value,label_zh,label_en,sort_order) VALUES
        ('1M+',   '活跃玩家',   'Active Players',   1),
        ('99.9%', '服务器稳定性','Server Uptime',    2),
        ('<5ms',  '指令响应延迟','Command Latency',   3),
        ('0',     '已知VAC误封', 'VAC False Bans',   4)
    ");

    // Matches
    $pdo->exec("INSERT INTO matches(team1_name,team1_flag,team2_name,team2_flag,score1,score2,map_name,status,elapsed) VALUES
        ('NAVI',    '🇺🇦','FaZe',    '🇪🇺',16, 11,'Mirage', 'LIVE',  '42:18'),
        ('G2',      '🇫🇷','Vitality','🇫🇷',9,  14,'Dust2',  'LIVE',  '31:55'),
        ('Astralis','🇩🇰','Liquid',  '🇺🇸',16,  7,'Inferno','ENDED', '52:10')
    ");

    // Features
    $features = [
        ['01 // SECURITY', '一键接入，VAC 级安全防御',
         '告别繁琐的控制台指令。深度接入 Steam 底层协议 (steam://)，完美支持 VAC 反作弊系统，一键直连 YACHIYO CUP 专属社区服。你只需要点击，系统完成剩下的一切。',
         'assets/feature_launcher.png', 'Steam 协议深度集成|VAC 反作弊白名单|一键社区服直连', 0, 1],
        ['02 // ANALYTICS', '数据可视化仪表盘',
         '实时同步玩家的 Rating、K/D Ratio、爆头率等高阶数据。内置每日电竞任务与动态进度条，你的每一场战斗都被精准记录，让进化清晰可见。',
         'assets/feature_dashboard.png', '实时 Rating & K/D|爆头率分析|每日任务系统', 1, 2],
        ['03 // EXPERIENCE', '千万级 UI 动效，电影级开场',
         '彻底摒弃传统软件的廉价感。每次启动伴随 2 秒的深渊浮现开场动画；软件内处处皆是流光溢彩与液态悬浮微动效，享受极致流畅的视觉盛宴。',
         'assets/feature_animation.png', '2s 深渊开场动画|液态悬浮微动效|全局流体过渡', 0, 3],
        ['04 // UPDATES', '无感静默更新 Smart Auto-Update',
         '内置专业的商业级更新核心。后台静默检测，专属进度条覆盖安装，彻底告别频繁访问官网下载更新包的烦恼。版本永远是最新，体验永远是最优。',
         'assets/feature_update.png', '后台静默下载|差量更新技术|一键覆盖安装', 0, 4],
        ['05 // ESPORTS', '赛事前瞻与 HLTV 级新闻流',
         '大屏 Banner 轮播赛事资讯，内置 HLTV 级别的实时比分追踪（如 NAVI vs FaZe），不错过任何一场顶尖对决。赛前分析、数据洞察、赛后复盘，一站搞定。',
         'assets/feature_news.png', '实时比分追踪|赛事 Banner 轮播|HLTV 级数据源', 0, 5],
    ];
    $stmt = $pdo->prepare("INSERT INTO features(tag,title,description,image,highlights,center_overlay,sort_order) VALUES(?,?,?,?,?,?,?)");
    foreach ($features as $f) $stmt->execute($f);
}

function get_setting(string $key, string $default = ''): string {
    $stmt = get_pdo()->prepare('SELECT v FROM settings WHERE k=?');
    $stmt->execute([$key]);
    $row = $stmt->fetch(PDO::FETCH_COLUMN);
    return $row !== false ? $row : $default;
}

function get_stats(): array {
    return get_pdo()->query('SELECT * FROM stats ORDER BY sort_order')->fetchAll(PDO::FETCH_ASSOC);
}

function get_matches(): array {
    return get_pdo()->query('SELECT * FROM matches ORDER BY id')->fetchAll(PDO::FETCH_ASSOC);
}

function get_features(): array {
    return get_pdo()->query('SELECT * FROM features ORDER BY sort_order')->fetchAll(PDO::FETCH_ASSOC);
}
