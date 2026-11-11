#!/bin/bash
# Ludex on Prometheus, from a fresh clone to a running Portainer stack:
#   .env (secrets generated, sa taken from shared-sql) → build on the NAS → apps.py → Portainer stack →
#   checks (database, ludex login, website) → Homepage card.
# Safe to run again: what's already done is kept (the .env, the apps.py line, the Homepage card).
#
#   sudo bash /volume3/docker/self-hosted/Ludex/deploy/prometheus-setup.sh
set -u
SELF=/volume3/docker/self-hosted
APP=$SELF/Ludex
SHARED=$SELF/shared-sql/.env
APPS_PY=/volume3/docker/_scripts/apps/apps.py
APPS_SH=/volume3/docker/_scripts/apps/apps.sh
HOMEPAGE=/volume3/docker/homepage
PORT=5098

ok()   { echo "   OK  $*"; }
fail() { echo "   !!! $*"; exit 1; }
step() { echo; echo "== $*"; }

[ "$(id -u)" = 0 ] || fail "corre com sudo: sudo bash $0"
cd "$APP" || fail "não encontro $APP (faz primeiro o git clone)"

step "código"
# No NAS o git corre num contentor (alpine/git) quando não está instalado.
if command -v git >/dev/null 2>&1; then
  git -c safe.directory="$APP" pull --ff-only 2>&1 | sed 's/^/   /'
else
  docker run --rm -v "$APP:/git" -w /git alpine/git -c safe.directory=/git pull --ff-only 2>&1 | sed 's/^/   /'
fi

step ".env"
[ -f "$SHARED" ] || fail "não encontro $SHARED"
gen() { echo "Aa1$(LC_ALL=C tr -dc 'A-Za-z0-9' </dev/urandom | head -c 24)"; }
get() { grep -m1 "^$1=" "$2" 2>/dev/null | cut -d= -f2- | sed "s/^['\"]//; s/['\"]$//"; }
SA=$(get SQL_SA_PASSWORD "$SHARED")
[ -n "$SA" ] || fail "o $SHARED não tem SQL_SA_PASSWORD"

# A password própria do login ludex fica também no .env do shared-sql, como a das outras apps.
APP_PW=$(get LUDEX_DB_PASSWORD "$SHARED")
if [ -z "$APP_PW" ]; then
  APP_PW=$(get SQL_APP_PASSWORD .env)
  [ -n "$APP_PW" ] || APP_PW=$(gen)
  printf '\nLUDEX_DB_PASSWORD=%s\n' "$APP_PW" >> "$SHARED"
  ok "LUDEX_DB_PASSWORD acrescentada a $SHARED"
fi

if [ -f .env ]; then
  ok ".env já existe: fica como está (só acerto o sa e a password do ludex)"
  sed -i "s|^SQL_SA_PASSWORD=.*|SQL_SA_PASSWORD=$SA|; s|^SQL_APP_PASSWORD=.*|SQL_APP_PASSWORD=$APP_PW|" .env
else
  cat > .env <<EOF
# Ludex — gerado por deploy/prometheus-setup.sh. Guarda estes valores no Vaultwarden.
SQL_SA_PASSWORD=$SA
SQL_APP_PASSWORD=$APP_PW
LUDEX_API_KEY=$(gen)
LUDEX_SETUP_KEY=$(gen)

# IGDB (https://dev.twitch.tv/console/apps): cola aqui o Client ID e o Client Secret e corre
#   sudo bash $APPS_SH atualizar ludex
IGDB_CLIENT_ID=
IGDB_CLIENT_SECRET=
EOF
  ok ".env criado"
fi
chmod 600 .env

step "porta $PORT"
if ss -ltn 2>/dev/null | grep -q ":$PORT " && ! docker ps --format '{{.Names}}' | grep -q '^ludex-website$'; then
  fail "a porta $PORT já está ocupada: acrescenta LUDEX_PORT=<outra> ao .env e corre outra vez"
fi
ok "livre"

step "a compilar no NAS (alguns minutos)"
if ! docker compose build > /tmp/ludex-build.log 2>&1; then
  tail -40 /tmp/ludex-build.log
  fail "a compilação falhou (log completo em /tmp/ludex-build.log). Nada foi alterado no Portainer."
fi
ok "imagens ludex-db-deploy, ludex-webapi e ludex-website"
docker volume inspect ludex_keys >/dev/null 2>&1 || docker volume create ludex_keys >/dev/null
ok "volume ludex_keys"

step "apps.py"
[ -f "$APPS_PY" ] || fail "não encontro $APPS_PY"
if grep -q '"ludex"' "$APPS_PY"; then
  ok "o Ludex já está lá"
else
  cp "$APPS_PY" "$APPS_PY.bak-$(date +%Y%m%d%H%M%S)"
  python3 - "$APPS_PY" <<'PY' || fail "não consegui acrescentar o Ludex ao apps.py"
import re, sys
p = sys.argv[1]; s = open(p).read()
m = re.search(r'^(\s*)"holo":[^\n]*\n', s, re.M) or re.search(r'^(\s*)"obol":[^\n]*\n', s, re.M)
if not m: sys.exit("no holo/obol line")
s = s[:m.end()] + f'{m.group(1)}"ludex":      ("Ludex",      "ludex"),\n' + s[m.end():]
s = re.sub(r'^(Apps: [^\n]*?)(\n)', lambda x: x.group(1) + ("" if "ludex" in x.group(1) else ", ludex") + x.group(2), s, count=1, flags=re.M)
open(p, "w").write(s)
PY
  python3 -m py_compile "$APPS_PY" || fail "o apps.py ficou inválido (cópia em $APPS_PY.bak-*)"
  ok "linha \"ludex\" acrescentada (cópia de segurança ao lado)"
fi

step "stack no Portainer"
if bash "$APPS_SH" 2>/dev/null | grep -qE '^ludex +sim'; then
  bash "$APPS_SH" atualizar ludex | sed 's/^/   /'
else
  bash "$APPS_SH" migrar ludex | sed 's/^/   /'
fi

step "verificações"
for i in $(seq 1 60); do
  state=$(docker inspect -f '{{.State.Status}} {{.State.ExitCode}}' ludex-db-deploy 2>/dev/null)
  [ "${state%% *}" = exited ] && break
  sleep 3
done
[ "$state" = "exited 0" ] || { docker logs ludex-db-deploy 2>&1 | tail -30; fail "o ludex-db-deploy não terminou bem ($state)"; }
ok "base de dados Ludex publicada"

SQLCMD="docker exec shared-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -C -b -h -1 -W"
n=$($SQLCMD -U ludex -P "$APP_PW" -d Ludex -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.procedures" 2>&1 | tr -d '[:space:]')
[[ "$n" =~ ^[0-9]+$ ]] && [ "$n" -ge 20 ] || fail "o login ludex não entra na base Ludex: $n"
ok "login ludex entra na Ludex ($n procedures)"
for other in Obol Milo Kardia LivingDex; do
  if $SQLCMD -U ludex -P "$APP_PW" -d "$other" -Q "SELECT 1" >/dev/null 2>&1; then
    fail "o login ludex consegue abrir a base $other (não devia)"
  fi
done
ok "e não consegue abrir as bases das outras apps"

for i in $(seq 1 40); do
  code=$(curl -s -o /dev/null -w '%{http_code}' "http://127.0.0.1:$PORT/create-account")
  [ "$code" = 200 ] && break
  sleep 3
done
[ "$code" = 200 ] || { docker logs ludex-webapi 2>&1 | tail -20; docker logs ludex-website 2>&1 | tail -20; fail "o site não responde (HTTP $code)"; }
ok "site a responder em http://<ip-do-nas>:$PORT"

step "Homepage"
add_card() {
  local f=$1
  [ -f "$f" ] || return 0
  if grep -q -- '- Ludex:' "$f"; then ok "$(basename "$(dirname "$f")")/services.yaml já tem o Ludex"; return 0; fi
  cp "$f" "$f.bak-$(date +%Y%m%d%H%M%S)"
  python3 - "$f" <<'PY' || { echo "   (não encontrei o cartão do Obol em $1: acrescenta o Ludex à mão)"; return 0; }
import re, sys
p = sys.argv[1]; s = open(p).read()
m = re.search(r'^( +)- Obol:\n((?:\1 +[^\n]*\n)+)', s, re.M)
if not m: sys.exit(1)
i, j = m.group(1), m.group(1) + "    "
card = (f"\n{i}- Ludex:\n{j}icon: mdi-gamepad-variant-#4fc3f7\n{j}href: http://{{{{HOMEPAGE_VAR_NAS}}}}:5098\n"
        f"{j}description: Biblioteca de jogos e amigos\n{j}container: ludex-website\n")
end = m.end()
s = s[:end] + card + s[end:]
open(p, "w").write(s)
PY
  ok "cartão do Ludex em $f"
}
add_card "$HOMEPAGE/config/services.yaml"
add_card "$HOMEPAGE/themes/shared/services.yaml"      # o use-theme.sh copia este para config
grep -q -- '- Ludex:' "$HOMEPAGE/config/services.yaml" 2>/dev/null && ok "o Homepage recarrega sozinho (refresca a página)"

echo
echo "Feito. Abre http://<ip-do-nas>:$PORT e cria a primeira conta com o setup key:"
echo "   $(get LUDEX_SETUP_KEY .env)"
echo "Guarda o .env no Vaultwarden:  sudo cat $APP/.env"
