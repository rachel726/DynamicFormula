const sql = require('mssql');
const math = require('mathjs');
const { performance } = require('perf_hooks');

const config = {
    server:   'DESKTOP-H70Q0IN\\SQLEXPRESS',
    database: 'DynamicFormula',
    options: {
        trustServerCertificate: true,
        instanceName:           'SQLEXPRESS',
        useUTC:                 false
    },
    authentication: {
    type: 'default',
    options: {
        userName: 'formula_user',
        password: 'Formula123!'
    }
},
    pool: { max: 10, min: 0, idleTimeoutMillis: 30000 },
    requestTimeout: 600000,
    connectionTimeout: 30000
};

const METHOD_NAME = 'NODEJS';

function normalize(expr) {
    return expr
        .replace(/power\s*\(/gi, 'pow(')
        .replace(/(?<![=<>!])=(?!=)/g, '==');
}

function compileFormula(f) {
    if (f.tnai && f.targilFalse) {
        const cond  = math.compile(normalize(f.tnai));
        const trueE = math.compile(normalize(f.targil));
        const falsE = math.compile(normalize(f.targilFalse));
        return (row) => cond.evaluate(row)
            ? Number(trueE.evaluate(row))
            : Number(falsE.evaluate(row));
    }
    const code = math.compile(normalize(f.targil));
    return (row) => Number(code.evaluate(row));
}

async function main() {
    console.log('');
    console.log('????????????????????????????????????????????????????????????????');
    console.log('?       ? Dynamic Formula Engine — Node.js Calculator ?       ?');
    console.log('?       Method 3: JavaScript + math.js (server-side)           ?');
    console.log('????????????????????????????????????????????????????????????????');
    console.log('');

    const totalStart = performance.now();

    try {
        console.log('? Connecting to SQL Server...');
        const pool = await sql.connect(config);
        console.log('  ? Connected\n');

        // ניקוי
        await pool.request().query(`
            DELETE FROM dbo.t_results WHERE method = '${METHOD_NAME}';
            DELETE FROM dbo.t_log WHERE method = '${METHOD_NAME}';`);

        // טעינת נוסחאות
        const formulaResult = await pool.request().query(`
            SELECT targil_id AS targilId, targil, tnai,
                   targil_false AS targilFalse, description
            FROM dbo.t_targil ORDER BY targil_id`);
        const formulas = formulaResult.recordset;
        console.log(`? Loaded ${formulas.length} formulas`);

        // טעינת נתונים
        process.stdout.write('? Loading data... ');
        const t0 = performance.now();
        const dataResult = await pool.request().query(`
            SELECT data_id AS dataId, a, b, c, d
            FROM dbo.t_data WITH (NOLOCK) ORDER BY data_id`);
        const data = dataResult.recordset;
        console.log(`${((performance.now()-t0)/1000).toFixed(2)}s (${data.length.toLocaleString()} rows)\n`);

        const logs = [];

        for (const f of formulas) {
            let compiled;
            try { compiled = compileFormula(f); }
            catch (err) {
                console.log(`  ? #${String(f.targilId).padStart(2,'0')} compile error: ${err.message}`);
                continue;
            }

            const results = new Array(data.length);
            const runStart = performance.now();
            for (let i = 0; i < data.length; i++) results[i] = compiled(data[i]);
            const runTimeSec = (performance.now() - runStart) / 1000;

            // שמירה ב-batches
            const BATCH = 10000;
            for (let b = 0; b < data.length; b += BATCH) {
                const table = new sql.Table('dbo.t_results');
                table.create = false;
                table.columns.add('data_id',   sql.Int,         { nullable: false });
                table.columns.add('targil_id', sql.Int,         { nullable: false });
                table.columns.add('method',    sql.VarChar(50), { nullable: false });
                table.columns.add('result',    sql.Float,       { nullable: true  });
                const end = Math.min(b + BATCH, data.length);
                for (let i = b; i < end; i++)
                    table.rows.add(data[i].dataId, f.targilId, METHOD_NAME, results[i]);
                await pool.request().bulk(table);
            }

            await pool.request()
                .input('tid',  sql.Int,         f.targilId)
                .input('m',    sql.VarChar(50), METHOD_NAME)
                .input('rt',   sql.Float,       runTimeSec)
                .input('rc',   sql.Int,         data.length)
                .query(`INSERT INTO dbo.t_log (targil_id,method,run_time,rows_count)
                        VALUES (@tid,@m,@rt,@rc)`);

            logs.push({ targilId: f.targilId, runTimeSec });
            console.log(`  ? #${String(f.targilId).padStart(2,'0')}  ${runTimeSec.toFixed(3).padStart(7)}s   (${data.length.toLocaleString()} rows)   ${f.description}`);
        }

        const avg = logs.reduce((s,l)=>s+l.runTimeSec,0)/logs.length;
        console.log('');
        console.log('????????????????????????????????????????????????????????????????');
        console.log(`?  Formulas: ${logs.length}   Avg: ${avg.toFixed(3)}s   Total: ${((performance.now()-totalStart)/1000).toFixed(2)}s`);
        console.log(`?  ? Saved to DB (method = '${METHOD_NAME}')`);
        console.log('????????????????????????????????????????????????????????????????');

    } catch(err) {
        console.error('? Fatal error:', err.message);
        process.exit(1);
    } finally {
        await sql.close();
    }
}

main();


