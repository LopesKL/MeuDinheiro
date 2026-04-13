import { useEffect, useState, useCallback, useMemo } from 'react';
import {
  Card,
  Col,
  Row,
  Statistic,
  Spin,
  Typography,
  Button,
  Space,
  Table,
  Popover,
  ColorPicker,
} from 'antd';
import { LeftOutlined, RightOutlined, BgColorsOutlined } from '@ant-design/icons';
import {
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  Legend,
  CartesianGrid,
  PieChart,
  Pie,
  Cell,
} from 'recharts';
import dayjs from 'dayjs';
import 'dayjs/locale/pt-br';
import { financeApi } from '@services/financeApi';
import { App } from 'antd';

dayjs.locale('pt-br');

const { Title, Text } = Typography;

const formatMoney = (v) =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(Number(v || 0));

/** Paleta quando o cartão não tem cor salva */
const CARD_SWATCHES = [
  '#1677ff',
  '#52c41a',
  '#faad14',
  '#ff7875',
  '#722ed1',
  '#13c2c2',
  '#eb2f96',
  '#fa8c16',
];

function isValidHexColor(s) {
  return typeof s === 'string' && /^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$/.test(s.trim());
}

function resolveCardColor(card, indexInList) {
  const t = card?.themeColor?.trim();
  if (isValidHexColor(t)) return t;
  return CARD_SWATCHES[indexInList % CARD_SWATCHES.length];
}

function categoryKey(name) {
  const s = name != null && String(name).trim() !== '' ? String(name).trim() : 'Outros';
  return s.toLowerCase();
}

function expensesForCategory(expenseLines, categoryName) {
  const want = categoryKey(categoryName);
  return (expenseLines || []).filter((e) => categoryKey(e.categoryName) === want);
}

/** Agrupa linhas de despesa por categoria (para modo filtrado por cartão). */
function buildSlicesFromExpenseLines(lines) {
  const buckets = new Map();
  for (const e of lines || []) {
    const raw = e.categoryName?.trim() || 'Outros';
    const key = raw.toLowerCase();
    if (!buckets.has(key)) {
      buckets.set(key, { categoryName: raw, amount: 0, items: [] });
    }
    const b = buckets.get(key);
    const amt = Number(e.amount || 0);
    b.amount += amt;
    b.items.push({
      kind: 'expense',
      title: e.description?.trim() ? e.description.trim() : 'Despesa',
      amount: amt,
      date: e.date,
    });
  }
  return [...buckets.values()].sort((a, b) => b.amount - a.amount);
}

const BREAKDOWN_KIND_LABELS = {
  expense: 'Despesa',
  installment: 'Parcela',
  recurring: 'Recorrente',
};

const PIE_COLORS = [
  '#1677ff',
  '#52c41a',
  '#faad14',
  '#ff7875',
  '#722ed1',
  '#13c2c2',
  '#eb2f96',
  '#fa8c16',
  '#2f54eb',
  '#a0d911',
];

const Dashboard = () => {
  const { message } = App.useApp();
  const [loading, setLoading] = useState(true);
  const [data, setData] = useState(null);
  const [creditCards, setCreditCards] = useState([]);
  const [selectedCardId, setSelectedCardId] = useState(null);
  const [selectedMonth, setSelectedMonth] = useState(() => dayjs().startOf('month'));
  const [expenseLines, setExpenseLines] = useState([]);
  const [expenseLinesLoading, setExpenseLinesLoading] = useState(false);
  const [cardFlowLoading, setCardFlowLoading] = useState(false);
  const [cardFlowSums, setCardFlowSums] = useState(null);
  const [colorPopoverCardId, setColorPopoverCardId] = useState(null);
  const [pickColor, setPickColor] = useState('#1677ff');
  const [savingColor, setSavingColor] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [d, cc] = await Promise.all([
        financeApi.dashboard(selectedMonth.year(), selectedMonth.month() + 1),
        financeApi.creditCards(),
      ]);
      setData(d);
      setCreditCards(Array.isArray(cc) ? cc : []);
    } catch (e) {
      message.error(e.message || 'Falha ao carregar dashboard');
    } finally {
      setLoading(false);
    }
  }, [message, selectedMonth]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    let cancelled = false;
    setExpenseLinesLoading(true);
    setExpenseLines([]);
    (async () => {
      try {
        const list = await financeApi.expenses(selectedMonth.year(), selectedMonth.month() + 1);
        if (!cancelled) setExpenseLines(Array.isArray(list) ? list : []);
      } catch (e) {
        if (!cancelled) {
          message.error(e.message || 'Erro ao carregar lançamentos');
          setExpenseLines([]);
        }
      } finally {
        if (!cancelled) setExpenseLinesLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [selectedMonth, message]);

  useEffect(() => {
    if (!selectedCardId) {
      setCardFlowSums(null);
      return undefined;
    }
    setCardFlowSums(null);
    let cancelled = false;
    const base = selectedMonth;
    const periods = [-1, 0, 1].map((i) => base.add(i, 'month'));
    setCardFlowLoading(true);
    Promise.all(periods.map((p) => financeApi.expenses(p.year(), p.month() + 1)))
      .then(([prev, curr, next]) => {
        if (cancelled) return;
        const fid = String(selectedCardId);
        const sum = (arr) =>
          (arr || [])
            .filter((e) => e.creditCardId != null && String(e.creditCardId) === fid)
            .reduce((s, e) => s + Number(e.amount || 0), 0);
        setCardFlowSums({ prev: sum(prev), curr: sum(curr), next: sum(next) });
      })
      .catch(() => {
        if (!cancelled) setCardFlowSums({ prev: 0, curr: 0, next: 0 });
      })
      .finally(() => {
        if (!cancelled) setCardFlowLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [selectedCardId, selectedMonth]);

  const goPrevMonth = () => setSelectedMonth((m) => m.subtract(1, 'month').startOf('month'));
  const goNextMonth = () => setSelectedMonth((m) => m.add(1, 'month').startOf('month'));

  const MAX_MONTHS_AHEAD = 120;
  const canGoNext = selectedMonth.isBefore(dayjs().add(MAX_MONTHS_AHEAD, 'month'), 'month');
  const monthLabel = selectedMonth.format('MMMM [de] YYYY');

  const filteredExpenseLines = useMemo(() => {
    if (!selectedCardId) return expenseLines;
    const fid = String(selectedCardId);
    return (expenseLines || []).filter(
      (e) => e.creditCardId != null && String(e.creditCardId) === fid
    );
  }, [expenseLines, selectedCardId]);

  const filteredMonthExpense = useMemo(
    () => filteredExpenseLines.reduce((s, e) => s + Number(e.amount || 0), 0),
    [filteredExpenseLines]
  );

  const selectedCardIndex = useMemo(
    () => creditCards.findIndex((c) => String(c.id) === String(selectedCardId)),
    [creditCards, selectedCardId]
  );
  const accentBarColor =
    selectedCardId && selectedCardIndex >= 0
      ? resolveCardColor(creditCards[selectedCardIndex], selectedCardIndex)
      : '#ff7875';

  const chartData = useMemo(() => {
    const flow = data?.lastMonthsFlow || [];
    const rowName = (m, idx) =>
      idx === 0
        ? `${m.label} (anterior)`
        : idx === 1
          ? `${m.label} (atual)`
          : idx === 2
            ? `${m.label} (próximo)`
            : m.label;

    if (!selectedCardId) {
      return flow.map((m, idx) => ({
        name: rowName(m, idx),
        Renda: Number(m.income),
        Gastos: Number(m.expense),
      }));
    }
    if (!cardFlowSums) {
      return flow.map((m, idx) => ({
        name: rowName(m, idx),
        Renda: Number(m.income),
        Gastos: 0,
      }));
    }
    const gastosSeq = [cardFlowSums.prev, cardFlowSums.curr, cardFlowSums.next];
    return flow.map((m, idx) => ({
      name: rowName(m, idx),
      Renda: Number(m.income),
      Gastos: Number(gastosSeq[idx] ?? 0),
    }));
  }, [data, selectedCardId, cardFlowSums]);

  const pieData = useMemo(() => {
    if (selectedCardId) {
      return buildSlicesFromExpenseLines(filteredExpenseLines).map((x) => ({
        name: x.categoryName,
        value: Number(x.amount),
      }));
    }
    return (data?.monthExpensesByCategory || []).map((x) => ({
      name: x.categoryName,
      value: Number(x.amount),
    }));
  }, [data, selectedCardId, filteredExpenseLines]);

  const categoryTableRows = useMemo(() => {
    if (selectedCardId) {
      const slices = buildSlicesFromExpenseLines(filteredExpenseLines);
      const total = filteredMonthExpense;
      return slices.map((item, i) => ({
        key: `f-${item.categoryName}-${i}`,
        categoryName: item.categoryName,
        amount: item.amount,
        pct: total > 0 ? (item.amount / total) * 100 : 0,
        breakdownItems: item.items,
      }));
    }
    const rows = data?.monthExpensesByCategory || [];
    const total = Number(data?.monthExpense) || 0;
    return rows.map((item, i) => {
      const amt = Number(item.amount) || 0;
      return {
        key: `${item.categoryName ?? 'cat'}-${i}`,
        categoryName: item.categoryName,
        amount: amt,
        pct: total > 0 ? (amt / total) * 100 : 0,
        breakdownItems: Array.isArray(item.items) ? item.items : [],
      };
    });
  }, [data, selectedCardId, filteredExpenseLines, filteredMonthExpense]);

  const displayMonthExpense = selectedCardId ? filteredMonthExpense : Number(data?.monthExpense) || 0;
  const displayMonthBalance = (Number(data?.monthIncome) || 0) - displayMonthExpense;

  const saveCardColor = async (card, hex) => {
    setSavingColor(true);
    try {
      await financeApi.creditCardUpsert({
        id: card.id,
        name: card.name,
        closingDay: card.closingDay,
        dueDay: card.dueDay,
        isMealVoucher: !!card.isMealVoucher,
        mealVoucherDailyAmount: card.mealVoucherDailyAmount ?? null,
        mealVoucherCreditDay: card.mealVoucherCreditDay ?? null,
        themeColor: hex,
      });
      message.success('Cor do cartão salva');
      setColorPopoverCardId(null);
      await load();
    } catch (e) {
      message.error(e.message || 'Erro ao salvar cor');
    } finally {
      setSavingColor(false);
    }
  };

  const categoryByExpenseColumns = [
    {
      title: 'Categoria',
      dataIndex: 'categoryName',
      key: 'categoryName',
      ellipsis: true,
      render: (t) => t || '—',
    },
    {
      title: '% do total',
      dataIndex: 'pct',
      key: 'pct',
      width: 108,
      align: 'right',
      render: (pct) => (pct >= 0.05 ? `${pct.toFixed(1)}%` : '—'),
    },
    {
      title: 'Valor',
      dataIndex: 'amount',
      key: 'amount',
      width: 120,
      align: 'right',
      render: (v) => formatMoney(v),
    },
  ];

  const expenseLinesColumns = [
    {
      title: 'Data',
      dataIndex: 'date',
      width: 96,
      render: (d) => (d ? dayjs(d).format('DD/MM/YYYY') : '—'),
    },
    {
      title: 'Categoria',
      dataIndex: 'categoryName',
      width: 140,
      ellipsis: true,
      render: (t) => t || '—',
    },
    {
      title: 'Descrição',
      dataIndex: 'description',
      ellipsis: true,
      render: (t) => (t && String(t).trim() ? t : '—'),
    },
    {
      title: 'Valor',
      dataIndex: 'amount',
      width: 108,
      align: 'right',
      render: (v) => formatMoney(v),
    },
  ];

  const categoryBreakdownColumns = [
    {
      title: 'Tipo',
      dataIndex: 'kind',
      key: 'kind',
      width: 100,
      render: (k) => BREAKDOWN_KIND_LABELS[k] || k || '—',
    },
    {
      title: 'Data',
      dataIndex: 'date',
      key: 'date',
      width: 96,
      render: (d) => (d ? dayjs(d).format('DD/MM/YYYY') : '—'),
    },
    {
      title: 'Descrição',
      dataIndex: 'title',
      key: 'title',
      ellipsis: true,
      render: (t) => (t && String(t).trim() ? t : '—'),
    },
    {
      title: 'Valor',
      dataIndex: 'amount',
      key: 'amount',
      width: 108,
      align: 'right',
      render: (v) => formatMoney(v),
    },
  ];

  return (
    <div>
      <Space direction="vertical" size={12} style={{ width: '100%', marginBottom: 16 }}>
        <Title level={3} style={{ marginTop: 0, marginBottom: 0 }}>
          Visão geral
        </Title>
        <div
          style={{
            display: 'flex',
            flexWrap: 'wrap',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: 12,
            padding: '10px 14px',
            background: 'var(--ant-color-fill-quaternary, #fafafa)',
            borderRadius: 8,
            border: '1px solid var(--ant-color-border-secondary, #f0f0f0)',
          }}
        >
          <Space align="center" wrap size={8}>
            <Button type="text" icon={<LeftOutlined />} onClick={goPrevMonth} aria-label="Mês anterior" />
            <Text strong style={{ fontSize: 16, minWidth: 160, textAlign: 'center', display: 'inline-block' }}>
              {monthLabel}
            </Text>
            <Button
              type="text"
              icon={<RightOutlined />}
              onClick={goNextMonth}
              disabled={!canGoNext}
              aria-label="Próximo mês"
            />
          </Space>
          {creditCards.length > 0 ? (
            <div
              style={{
                display: 'flex',
                flexWrap: 'wrap',
                alignItems: 'center',
                justifyContent: 'flex-end',
                gap: 8,
                flex: '1 1 240px',
                minWidth: 0,
              }}
            >
              <Space wrap size={[8, 8]} align="center" style={{ justifyContent: 'flex-end' }}>
                <Button type={!selectedCardId ? 'primary' : 'default'} onClick={() => setSelectedCardId(null)}>
                  Todos
                </Button>
                {creditCards.map((c, idx) => {
                  const col = resolveCardColor(c, idx);
                  const open = colorPopoverCardId === c.id;
                  return (
                    <div
                      key={c.id}
                      style={{
                        display: 'inline-flex',
                        alignItems: 'stretch',
                        borderRadius: 8,
                        overflow: 'hidden',
                        border: `1px solid ${selectedCardId === c.id ? col : 'var(--ant-color-border, #d9d9d9)'}`,
                      }}
                    >
                      <Button
                        type={selectedCardId === c.id ? 'primary' : 'default'}
                        onClick={() => setSelectedCardId((cur) => (cur === c.id ? null : c.id))}
                        style={{
                          borderRadius: 0,
                          border: 'none',
                          boxShadow: 'none',
                          borderLeft: `4px solid ${col}`,
                        }}
                      >
                        {c.name}
                      </Button>
                    </div>
                  );
                })}
              </Space>
            </div>
          ) : null}
        </div>
      </Space>

      {selectedCardId ? (
        <Text type="secondary" style={{ display: 'block', marginBottom: 12 }}>
          Gráficos de gastos e tabelas abaixo refletem só despesas lançadas neste cartão. Renda e patrimônio
          continuam globais.
        </Text>
      ) : null}

      <Spin spinning={loading}>
        <Row gutter={[16, 16]}>
          <Col xs={24} sm={12} lg={6}>
            <Card>
              <Statistic title="Renda no mês" value={data?.monthIncome} formatter={formatMoney} />
            </Card>
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <Card>
              <Statistic
                title={
                  selectedCardId
                    ? `Gastos no cartão (${selectedMonth.format('MM/YYYY')})`
                    : `Gastos no mês (${selectedMonth.format('MM/YYYY')})`
                }
                value={displayMonthExpense}
                formatter={formatMoney}
              />
            </Card>
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <Card>
              <Statistic
                title={selectedCardId ? 'Saldo (renda − gastos do cartão)' : 'Saldo do mês'}
                value={displayMonthBalance}
                formatter={formatMoney}
                valueStyle={{ color: displayMonthBalance >= 0 ? '#3f8600' : '#cf1322' }}
              />
            </Card>
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <Card>
              <Statistic title="Patrimônio líquido (contas)" value={data?.totalPatrimony} formatter={formatMoney} />
            </Card>
          </Col>
        </Row>
        <Row gutter={[16, 16]} style={{ marginTop: 24 }}>
          <Col xs={24} lg={14}>
            <Card
              title={`Renda x gastos — mês anterior, atual (${selectedMonth.format('MM/YYYY')}) e próximo`}
              extra={
                selectedCardId ? (
                  <Text type="secondary" style={{ fontSize: 12 }}>
                    Barras de gasto = só este cartão
                  </Text>
                ) : null
              }
            >
              <Spin spinning={!!selectedCardId && cardFlowLoading}>
                <div style={{ width: '100%', height: 340 }}>
                  <ResponsiveContainer>
                    <BarChart data={chartData} margin={{ top: 8, right: 8, left: 0, bottom: 8 }}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="name" interval={0} angle={-22} textAnchor="end" height={56} />
                      <YAxis />
                      <Tooltip formatter={(v) => formatMoney(v)} />
                      <Legend />
                      <Bar dataKey="Renda" fill="#52c41a" />
                      <Bar dataKey="Gastos" fill={accentBarColor} />
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              </Spin>
            </Card>
          </Col>
          <Col xs={24} lg={10}>
            <Card title={`Gastos do mês por categoria — ${selectedMonth.format('MM/YYYY')}`}>
              {pieData.length === 0 ? (
                <div style={{ height: 340, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  <Text type="secondary">
                    {selectedCardId ? 'Nenhum gasto neste cartão neste mês' : 'Nenhum gasto neste mês'}
                  </Text>
                </div>
              ) : (
                <div style={{ width: '100%', height: 340 }}>
                  <ResponsiveContainer>
                    <PieChart>
                      <Pie
                        data={pieData}
                        dataKey="value"
                        nameKey="name"
                        cx="50%"
                        cy="50%"
                        outerRadius={118}
                        paddingAngle={1}
                        label={({ percent }) => (percent >= 0.06 ? `${(percent * 100).toFixed(0)}%` : '')}
                      >
                        {pieData.map((_, index) => (
                          <Cell key={`slice-${index}`} fill={PIE_COLORS[index % PIE_COLORS.length]} />
                        ))}
                      </Pie>
                      <Tooltip formatter={(v) => formatMoney(v)} />
                      <Legend layout="horizontal" verticalAlign="bottom" />
                    </PieChart>
                  </ResponsiveContainer>
                </div>
              )}
            </Card>
          </Col>
        </Row>
        <Row gutter={[16, 16]} style={{ marginTop: 24 }}>
          <Col xs={24} lg={12}>
            <Card title={`Gastos por categoria — ${selectedMonth.format('MM/YYYY')}`}>
              <Table
                size="small"
                pagination={false}
                scroll={{ x: true }}
                rowKey="key"
                dataSource={categoryTableRows}
                locale={{ emptyText: 'Nenhuma categoria neste mês' }}
                columns={categoryByExpenseColumns}
                expandable={{
                  expandedRowRender: (record) => {
                    const apiItems = record.breakdownItems || [];
                    if (apiItems.length > 0) {
                      return (
                        <div style={{ padding: '8px 8px 12px 40px', background: '#fafafa' }}>
                          <Table
                            size="small"
                            pagination={false}
                            rowKey={(_, idx) => `bd-${record.key}-${idx}`}
                            dataSource={apiItems}
                            columns={categoryBreakdownColumns}
                            locale={{ emptyText: '—' }}
                          />
                        </div>
                      );
                    }
                    if (expenseLinesLoading) {
                      return (
                        <div style={{ padding: '16px 24px', background: '#fafafa' }}>
                          <Spin size="small" /> <Text type="secondary">Carregando lançamentos…</Text>
                        </div>
                      );
                    }
                    const linesSrc = selectedCardId ? filteredExpenseLines : expenseLines;
                    const lines = expensesForCategory(linesSrc, record.categoryName);
                    return (
                      <div style={{ padding: '8px 8px 12px 40px', background: '#fafafa' }}>
                        {lines.length === 0 ? (
                          <Text type="secondary">
                            Nenhum detalhe disponível. Atualize a API ou verifique se o valor vem só de regras
                            antigas de resumo.
                          </Text>
                        ) : (
                          <Table
                            size="small"
                            pagination={false}
                            rowKey="id"
                            dataSource={lines}
                            columns={[
                              {
                                title: 'Data',
                                dataIndex: 'date',
                                width: 96,
                                render: (d) => (d ? dayjs(d).format('DD/MM/YYYY') : '—'),
                              },
                              {
                                title: 'Descrição',
                                dataIndex: 'description',
                                ellipsis: true,
                                render: (t) => (t && String(t).trim() ? t : '—'),
                              },
                              {
                                title: 'Valor',
                                dataIndex: 'amount',
                                width: 108,
                                align: 'right',
                                render: (v) => formatMoney(v),
                              },
                            ]}
                            locale={{ emptyText: '—' }}
                          />
                        )}
                      </div>
                    );
                  },
                }}
              />
            </Card>
          </Col>
          <Col xs={24} lg={12}>
            <Card
              title={
                selectedCardId
                  ? `Lançamentos do cartão — ${selectedMonth.format('MM/YYYY')}`
                  : `Lançamentos registrados — ${selectedMonth.format('MM/YYYY')}`
              }
            >
              <Spin spinning={expenseLinesLoading}>
                <Table
                  size="small"
                  pagination={false}
                  scroll={{ x: true }}
                  rowKey="id"
                  dataSource={selectedCardId ? filteredExpenseLines : expenseLines}
                  locale={{
                    emptyText: selectedCardId
                      ? 'Nenhum lançamento neste cartão neste mês'
                      : 'Nenhum lançamento neste mês',
                  }}
                  columns={expenseLinesColumns}
                />
              </Spin>
            </Card>
          </Col>
        </Row>
      </Spin>
    </div>
  );
};

export default Dashboard;
